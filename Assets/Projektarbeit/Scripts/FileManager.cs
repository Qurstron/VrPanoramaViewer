using GLTFast;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using static JSONClasses;

public class FileManager : MonoBehaviour
{
    public TimelineController timelineController;
    public GraphUI graphUI;
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
            Debug.LogErrorFormat("Failed to load Overview for {0} error: {1}", url, www.error);
            callbackFailure?.Invoke();
            yield break;
        }

        string text = www.downloadHandler.text;
        text = "{\"Items\":" + text + "}";
        var pains = JsonHelper.FromJson<PanoramaMenuEntry>(text);

        callback?.Invoke(pains);
    }
    public IEnumerator LoadThumbnail(string url, Action<Texture2D> callback = null, Action callbackFailure = null)
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            callbackFailure?.Invoke();
            yield break;
        }

        callback?.Invoke(((DownloadHandlerTexture)www.downloadHandler).texture);
    }
    public IEnumerator SavePanorama(string url, string name, Action callback = null, Action callbackFailure = null)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            callbackFailure?.Invoke();
            yield break;
        }

        string folderPath = Path.Combine(Application.persistentDataPath, name);
        string zipPath = folderPath + ".zip";

        if (!Directory.Exists(folderPath))
        {
            File.WriteAllBytes(zipPath, www.downloadHandler.data);
            ZipFile.ExtractToDirectory(zipPath, folderPath);
            File.Delete(zipPath);
        }

        callback?.Invoke();
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
    // returns null if Panorama wasn't found or is otherwise not available/readeble
    public async Task<Config> LoadLocalPanorama(string name)
    {
        string folderPath = GetFolderPath(name);
        if (!Directory.Exists(folderPath))
        {
            Debug.LogErrorFormat("{0} dosn't exists", folderPath);
            return null;
        }

        Config config = null;
        try
        {
            config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(folderPath + "/config.json"));
            config.Construct(name);
            config.folderPath = folderPath;
        }
        catch (Exception e)
        {
            Debug.LogErrorFormat("{0} couldn't be correctly loaded, error: {1}", folderPath, e.Message);
            return null;
        }

        // Selected panorama is already loaded
        if (config.Equals(graphUI.Config))
        {
            return graphUI.Config;
        }
        if (!ValidateAllPaths(config))
            throw new Exception("Paths in Config could not be fully validated");

        Dictionary<string, byte[]> contentData = new();
        Dictionary<string, GltfImport> gltfs = new();
        foreach (string texName in config.TextureNames)
        {
            contentData.Add(texName, await File.ReadAllBytesAsync(Path.Combine(folderPath, texName)));
        }
        foreach (string objPath in config.ObjectsPaths)
        {
            gltfs.Add(objPath, await LoadGltfBinaryFromMemory(Path.Combine(folderPath, objPath)));
        }
        panoramaSphereController.config = config;
        panoramaSphereController.ContentData = contentData;
        panoramaSphereController.Gltfs = gltfs;

        if (watcher != null) watcher.Dispose();
        watcher = new FileSystemWatcher();
        watcher.Path = folderPath;
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
        watcher.Filter = "*.*";
        watcher.Changed += FileChanged;
        watcher.EnableRaisingEvents = true;

        graphUI.SetConfig(config);

        Debug.Log($"config {config.name} loaded");
        return config;
    }

    public string GetFolderPath(string name)
    {
        return Path.Combine(Application.persistentDataPath, name);
    }

    private async Task<GltfImport> LoadGltfBinaryFromMemory(string path)
    {
        byte[] data = await File.ReadAllBytesAsync(path);
        var gltf = new GltfImport();
        // https://stackoverflow.com/questions/75613904/unity-gltfast-cant-load-my-gltf-file-with-loadgltfbinary-function
        bool success = await gltf.Load(data, new Uri(path));
        if (success)
        {
            return gltf;
        }

        Debug.LogError($"could not load {path}");
        return null;
    }

    // TODO: fix
    // returns true if all paths in config are exists and are inside panorama folder
    private bool ValidateAllPaths(Config config)
    {
        string folderPath = Path.Combine(Application.persistentDataPath, config.name);

        try
        {
            foreach (NodeContent cat in config.AllNodeContents())
            {
                if (string.IsNullOrEmpty(cat.texture)) continue;
                var file = Directory.GetFiles(folderPath, cat.texture, SearchOption.AllDirectories).FirstOrDefault();
                if (file == null) return false;
            }
        }
        catch
        {
            return false;
        }

        return true;
    }
    private void FileChanged(object o, FileSystemEventArgs e)
    {
        Debug.Log($"file change in {e.FullPath}, Type: {e.ChangeType}");
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
            foreach (string texName in graphUI.Config.TextureNames.Except(newNames))
            {
                panoramaSphereController.ContentData.Add(texName, File.ReadAllBytes(folderpath + "/" + texName));
            }
            // remove obsolete images
            foreach (string texName in newNames.Except(graphUI.Config.TextureNames))
            {
                panoramaSphereController.ContentData.Remove(texName);
            }

            //timelineController.Fill(config, true); // this will update the panoramaSphereController 
            graphUI.SetConfig(config, true);
            return;
        }
        // Image change
        if (panoramaSphereController.ContentData.ContainsKey(filename))
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
}
