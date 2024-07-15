using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using static JSONClasses;

public class DownloadTexture : MonoBehaviour
{
    public TimelineController timelineController;
    public PanoramaSphereController panoramaSphereController;

    private FileSystemWatcher watcher = null;
    private FileSystemEventArgs lastArgs = null; // safe args for main thread
    private bool fileChanged = false; // signal main thread to update file
    // TODO: refactor
    // https://stackoverflow.com/questions/5678216/all-possible-array-initialization-syntaxes
    public static class JsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
            return wrapper.Items;
        }

        public static string ToJson<T>(T[] array)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper);
        }

        public static string ToJson<T>(T[] array, bool prettyPrint)
        {
            Wrapper<T> wrapper = new Wrapper<T>();
            wrapper.Items = array;
            return JsonUtility.ToJson(wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] Items;
        }
    }

    public IEnumerator LoadOverview(string url, Action<PanoramaMenuEntry[]> callback = null, Action callbackFailure = null)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            //Debug.Log(www.error);
            callbackFailure?.Invoke();
        }
        else
        {
            string text = www.downloadHandler.text;
            text = "{\"Items\":" + text + "}";
            var pains = JsonHelper.FromJson<PanoramaMenuEntry>(text);

            callback?.Invoke(pains);
        }
    }
    public IEnumerator LoadThumbnail(string url, Action<Texture2D> callback = null, Action callbackFailure = null)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            callbackFailure?.Invoke();
        }
        else
        {
            callback?.Invoke(((DownloadHandlerTexture)www.downloadHandler).texture);
        }
    }
    public IEnumerator SavePanorama(string url, string name, Action<Config> callback = null, Action callbackFailure = null)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            callbackFailure?.Invoke();
        }
        else
        {
            string folderPath = string.Format("{0}/{1}", Application.persistentDataPath, name);
            string zipPath = folderPath + ".zip";

            // if (Directory.Exists(folderPath)) Directory.Delete(folderPath, true);
            if (!Directory.Exists(folderPath))
            {
                File.WriteAllBytes(zipPath, www.downloadHandler.data);
                ZipFile.ExtractToDirectory(zipPath, folderPath);
                File.Delete(zipPath);
            }

            //Doc doc = JsonUtility.FromJson<Doc>(File.ReadAllText(folderPath + "/config.json"));
            //Debug.Log(doc);
            //texArray = doc.pics.Select(d =>
            //{
            //    var bytes = File.ReadAllBytes(folderPath + "/" + d.name);
            //    Texture2D texture = new Texture2D(1, 1);
            //    texture.LoadImage(bytes);

            //    return texture;
            //}).ToArray();
            //changePanorama(0);

            ////dropdown.AddOptions(doc.pics.Select(d => d.name).ToList());
            //timelineControl.GetComponent<TimelineController>().Fill(doc.pics, changePanorama);

            //callback?.Invoke(await LoadLocalPanorama(name));
        }
    }

    public List<PanoramaMenuEntry> GetLocalPanoramas()
    {
        List<PanoramaMenuEntry> result = new();

        foreach (string dir in Directory.GetDirectories(Application.persistentDataPath))
        {
            string dirName = Path.GetFileName(dir);
            string filePath = Path.Combine(dir, "config.json");

            try
            {
                // skips folders that dont have a config
                // this is very usefull on oculus quest devices (andriod devices in generell),
                // because there are folders in the persistentDataPath that are not panoramas
                if (!File.Exists(filePath)) continue;
                Config doc = JsonUtility.FromJson<Config>(File.ReadAllText(filePath));

                PanoramaMenuEntry entry = new PanoramaMenuEntry
                {
                    name = dirName,
                    size = -1,
                    version = doc.version
                };
                result.Add(entry);
            }
            catch
            {
                // probably a dircetory that we dont have access to
                // either way we dont care
                continue;
            }
        }

        return result;
    }
    public IEnumerator GetLocalThumbnail(string name, Action<Texture2D> callback)
    {
        yield return LoadThumbnail($"file://{Application.persistentDataPath}/{name}/thumbnail.jpg", callback);
    }
    public async Task<Config> LoadLocalPanorama(string name)
    {
        string folderPath = $"{Application.persistentDataPath}/{name}";
        
        Config config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(folderPath + "/config.json"));
        config.Construct(name);
        foreach (var pic in config.pics)
        {
            foreach (var cat in pic.categories)
            {
                foreach (var label in cat.labels)
                {
                    if (string.IsNullOrEmpty(label.details))
                    {
                        label.details = label.content;
                    }
                }
            }
        }

        // Selected panorama is already loaded
        if (config.Equals(timelineController.Config)) return timelineController.Config;

        Dictionary<string, byte[]> textureData = new();
        foreach (string texName in config.TextureNames)
        {
            textureData.Add(texName, await File.ReadAllBytesAsync(folderPath + "/" + texName));
        }
        panoramaSphereController.TextureData = textureData;

        if (watcher != null) watcher.Dispose();
        watcher = new FileSystemWatcher();
        watcher.Path = folderPath;
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        watcher.Filter = "*.*";
        watcher.Changed += FileChanged;
        watcher.EnableRaisingEvents = true;

        timelineController.Fill(config); // fun fact, this ends the function

        return config;
    }

    private void FileChanged(object o, FileSystemEventArgs e)
    {
        lastArgs = e;
        fileChanged = true;
    }
    private void Update()
    {
        if (!fileChanged) return;
        fileChanged = false;

        string filename = Path.GetFileName(lastArgs.FullPath);
        string folderpath = Path.GetDirectoryName(lastArgs.FullPath);

        // Config change
        if (filename.Equals("config.json"))
        {
            Config config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(lastArgs.FullPath));
            config.Construct(Path.GetFileName(lastArgs.FullPath));

            var newNames = config.TextureNames;
            // add new images
            foreach (string texName in timelineController.Config.TextureNames.Except(newNames))
            {
                panoramaSphereController.TextureData.Add(texName, File.ReadAllBytes(folderpath + "/" + texName));
            }
            // remove obsolete images
            foreach (string texName in newNames.Except(timelineController.Config.TextureNames))
            {
                panoramaSphereController.TextureData.Remove(texName);
            }

            timelineController.Fill(config, true); // this will update the panoramaSphereController 
            return;
        }
        // Image change
        if (panoramaSphereController.TextureData.ContainsKey(filename))
        {
            panoramaSphereController.UpdateTexture(filename, File.ReadAllBytes(lastArgs.FullPath));
            return;
        }

        Debug.Log("File changed in current panorama folder but isn't connected to panorama");
    }
    private void OnApplicationQuit()
    {
        watcher?.Dispose();
    }

    //public IEnumerator Upload()
    //{
    //    List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
    //    //formData.Add(new MultipartFormDataSection("field1=foo&field2=bar"));
    //    formData.Add(new MultipartFormFileSection("my file data", "myfile.txt"));

    //    UnityWebRequest www = UnityWebRequest.Post("https://localhost:7206/Content", formData);
    //    yield return www.SendWebRequest();

    //    if (www.isNetworkError || www.isHttpError)
    //    {
    //        Debug.Log(www.error);
    //    }
    //    else
    //    {
    //        Debug.Log("Form upload complete!");
    //    }
    //}
}
