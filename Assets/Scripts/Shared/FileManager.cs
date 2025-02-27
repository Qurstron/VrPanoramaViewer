using JSONClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class FileManager : MonoBehaviour
{
    [SerializeField] private PanoramaSphereController panoramaSphereController;
    [SerializeField] private Texture2D defaultThumbnail;
    [SerializeField] private bool watchCurrentConfig = true;

    public string persistentDataPath;
    private string tempDataPath;
    private FileSystemWatcher watcher = null;
    private FileSystemEventArgs lastArgs = null; // safe args for main thread
    private bool fileChanged = false; // signal main thread to update file
    private Config lastLoadedConfig = null;
    private static readonly System.Random random = new();
    public string LastLoadMsg { get; private set; }
    public PanoramaMenuEntry.Error LastLoadError { get; private set; }

    // https://discussions.unity.com/t/how-do-i-go-about-deserializing-a-json-array/179394
    public static class JsonArrayHelper
    {
        public static T[] FromJson<T>(string json)
        {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>($"{{\"Items\":{json}}}");
            return wrapper.Items;
        }
        public static string ToJson<T>(T[] array, bool prettyPrint = false)
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
    // modified version of:
    // https://stackoverflow.com/questions/11320968/can-newtonsoft-json-net-skip-serializing-empty-lists
    public class IgnoreEmptyEnumerableResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member,
            MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (property.PropertyType != typeof(string) &&
                typeof(IEnumerable).IsAssignableFrom(property.PropertyType))
            {
                property.ShouldSerialize = instance =>
                {
                    IEnumerable enumerable = null;
                    // this value could be in a public field or public property
                    switch (member.MemberType)
                    {
                        case MemberTypes.Property:
                            enumerable = instance
                                .GetType()
                                .GetProperty(member.Name)
                                ?.GetValue(instance, null) as IEnumerable;
                            break;
                        case MemberTypes.Field:
                            enumerable = instance
                                .GetType()
                                .GetField(member.Name)
                                .GetValue(instance) as IEnumerable;
                            break;
                    }

                    return enumerable == null ||
                           enumerable.GetEnumerator().MoveNext();
                    // if the list is null, we defer the decision to NullValueHandling
                };
            }

            return property;
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
        var ps = JsonArrayHelper.FromJson<PanoramaMenuEntry>(text);
        foreach (var entry in ps)
        {
            entry.path = url;
        }

        callback?.Invoke(ps);
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

    public async Task<PanoramaMenuEntry> GetLocalPanorama(string path)
    {
        try
        {
            string dirName = Path.GetFileName(path);
            string filePath = Path.Combine(path, "config.json");
            // skips folders that dont have a config
            // this is very usefull on oculus quest devices (andriod devices in generell),
            // because there are folders in the persistentDataPath that are not panoramas
            if (!File.Exists(filePath)) return null;
            Config config = await LoadLocalPanorama(path, true);

            PanoramaMenuEntry entry = new()
            {
                config = config,
                path = path,
                nodeCount = config.nodes.Count,
                size = DirSize(new DirectoryInfo(path)),
                lastEdited = File.GetLastAccessTime(filePath),
                thumbnail = defaultThumbnail,
            };
            return entry;
        }
        catch (ArgumentException ae)
        {
            return new()
            {
                error = PanoramaMenuEntry.Error.FileNotFound,
                path = path
            };
        }
        catch (Exception)
        {
            // probably a broken Config or a dircetory that we dont have access to
            return new()
            {
                error = PanoramaMenuEntry.Error.Deserialize,
                path = path
            };
        }
    }
    public async Task<List<PanoramaMenuEntry>> GetLocalPanoramasInDirectory(string path, bool filterError = false)
    {
        List<PanoramaMenuEntry> result = new();

        foreach (string dir in Directory.GetDirectories(path))
        {
            PanoramaMenuEntry entry = await GetLocalPanorama(dir);
            if (entry == null) continue;
            if (filterError && entry.HasError) continue;
            result.Add(entry);
        }

        return result;
    }
    public IEnumerator GetLocalThumbnail(string path, Action<Texture2D> callback)
    {
        if (File.Exists(Path.Combine(path, "thumbnail.png")))
        {
            yield return LoadThumbnail($"file://{path}/thumbnail.png", callback);
        }
        else if (File.Exists(Path.Combine(path, "thumbnail.jpg")))
        {
            yield return LoadThumbnail($"file://{path}/thumbnail.jpg", callback);
        }

        //throw new Exception($"can't find thumbnail in {path}");
    }
    /// <summary>
    /// Loads a Panorama from disk
    /// </summary>
    /// <param name="path">Full path to the panorama folder</param>
    /// <param name="isQuickRead">Indicates if the config construction and assets loading should be skiped</param>
    /// <returns>The loaded panorama, null if panorama can't be loaded</returns>
    public async Task<Config> LoadLocalPanorama(string path, bool isQuickRead = false)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                string err = $"{path} dosn't exists";
                Debug.LogError(err);
                LastLoadError = PanoramaMenuEntry.Error.FileNotFound;
                throw new ArgumentException(err);
            }

            try
            {
                JsonSerializerSettings settings = new()
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                    MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                };
                Config config = JsonConvert.DeserializeObject<Config>(await File.ReadAllTextAsync(path + "/config.json"), settings);
                config.path = path;

                if (isQuickRead) return config;
                if (config.Equals(lastLoadedConfig)) return lastLoadedConfig;
                return await LoadLocalPanorama(config);
            }
            catch (Exception e)
            {
                LastLoadError = PanoramaMenuEntry.Error.Deserialize;
                Debug.LogErrorFormat("{0} couldn't be correctly loaded, error: {1}", path, e.Message);
                return null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return null;
        }
    }
    public async Task<Config> LoadLocalPanorama(Config config)
    {
        string path = config.path;
        try
        {
            try
            {
                if (config.Equals(lastLoadedConfig)) return lastLoadedConfig;
                if (!config.Construct())
                {
                    LastLoadError = PanoramaMenuEntry.Error.Validation;
                    Debug.LogError($"{config.name} couldn't be constructed");
                    return null;
                }
            }
            catch (Exception e)
            {
                LastLoadError = PanoramaMenuEntry.Error.Undefined;
                LastLoadMsg = e.Message;
                Debug.LogErrorFormat("{0} couldn't be correctly loaded, error: {1}", path, e.Message);
                return null;
            }

            //if (!ValidateAllPaths(config))
            //{
            //    LastLoadErrorMsg = "Paths in Config could not be fully validated";
            //    throw new Exception(LastLoadErrorMsg);
            //}

            foreach (string texName in config.TextureNames)
            {
                config.contentData.Add(texName, await File.ReadAllBytesAsync(Path.Combine(path, texName)));
            }
            foreach (string objPath in config.ObjectsPaths)
            {
                config.contentData.Add(objPath, await File.ReadAllBytesAsync(Path.Combine(path, objPath)));
            }

            if (watchCurrentConfig)
            {
                watcher?.Dispose();
                watcher = new FileSystemWatcher
                {
                    Path = path,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    Filter = "*.*"
                };
                watcher.Changed += FileChanged;
                watcher.EnableRaisingEvents = true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            return null;
        }

        Debug.Log($"config {config.path} loaded");
        lastLoadedConfig = config;
        return config;
    }

    public string GetFolderPath(string name)
    {
        return Path.Combine(persistentDataPath, name);
    }

    public async Task SaveLocalPanorama(Config config)
    {
        //config.PrepareSave();
        await File.WriteAllTextAsync(Path.Combine(config.path, "config.json"), JsonConvert.SerializeObject(config, Formatting.None, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            ContractResolver = new IgnoreEmptyEnumerableResolver(),
            MissingMemberHandling = MissingMemberHandling.Ignore
        }));

        Debug.Log($"saved {config.path}");
    }
    /// <summary>
    /// Exports a project to a zip file
    /// </summary>
    /// <param name="path">Zip file path. Must be a Zip file and not a path to the destination of the Zip file</param>
    /// <param name="skipSave">Skips the saving of the project. Default behavior in most applications is true</param>
    /// <returns></returns>
    public async Task ExportLocalPanorama(Config config, string path, bool skipSave = true)
    {
        if (!skipSave)
            await SaveLocalPanorama(config);
        ZipFile.CreateFromDirectory(config.path, path);
    }
    public async Task ExportLocalPanoramaToServer(Config config, string url, string username, string password)
    {
        string tempZip = Path.Combine(tempDataPath, $"{new Guid()}.zip");
        await ExportLocalPanorama(config, tempZip);

        // upload to server
        //UnityWebRequest www = UnityWebRequest.Put("http://localhost:3000/api/rawCoords", jsonStringTrial);
        //www.SetRequestHeader("Content-Type", "application/json");

        File.Delete(tempZip);
    }
    public async Task<Config> CreateLocalPanorama(Template template, string name, string path, string thumbnailPath)
    {
        string folderPath;
        string thumbnailConfigPath;
        string templatePath = Path.Combine(Application.dataPath, "Templates", template.assetPath + ".zip");
        Config config;

        do
        {
            folderPath = Path.Combine(path, $"{name} - {random.Next()}");
        } while (Directory.Exists(folderPath));
        path = folderPath;
        thumbnailConfigPath = Path.Combine(folderPath, "thumbnail.png");

        Directory.CreateDirectory(path);
        Debug.Log($"Directroy {path} created");

        try
        {
            ZipFile.ExtractToDirectory(templatePath, folderPath);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            throw e;
        }
        if (string.IsNullOrEmpty(thumbnailPath))
        {
            await File.WriteAllBytesAsync(Path.Combine(path, "thumbnail.png"), defaultThumbnail.EncodeToPNG());
        }
        else
        {
            if (Path.GetExtension(thumbnailPath) == "png")
            {
                File.Copy(thumbnailPath, thumbnailConfigPath);
            }
            else
            {
                Texture2D tex = new(1, 1);
                tex.LoadImage(await File.ReadAllBytesAsync(thumbnailPath));
                await File.WriteAllBytesAsync(thumbnailConfigPath, tex.EncodeToPNG());
            }
        }

        config = await LoadLocalPanorama(folderPath);
        config.name = name;
        config.author = Environment.UserName;
        config.description = $"Created with Vr Panorama Editor version: {Application.version}";

        SaveLocalPanorama(config);

        return config;
    }
    //public async Task<string> CreateLocalPanorama(string path, string name)
    //{
    //    string folderPath;
    //    do
    //    {
    //        folderPath = Path.Combine(path, $"{name} - {random.Next()}");
    //    } while (Directory.Exists(folderPath));
    //    path = folderPath;

    //    Directory.CreateDirectory(path);
    //    Debug.Log($"Directroy {path} created");

    //    Config config = new(true)
    //    {
    //        name = "New Project",
    //        version = 1,
    //        isWip = true,
    //        author = Environment.UserName,
    //        description = $"Created with Vr Panorama Editor version: {Application.version}",
    //        root = "root",
    //        nodes = new()
    //        {
    //            new Node()
    //            {
    //                name = "Root",
    //                uniqueName = "root",
    //                description = "Root Node",
    //                color = "FFFFFF",
    //                content = new()
    //                {
    //                    new NodeContent()
    //                    {

    //                    }
    //                }
    //            }
    //        },
    //    };

    //    await File.WriteAllTextAsync(Path.Combine(path, "config.json"), JsonConvert.SerializeObject(config));
    //    await File.WriteAllBytesAsync(Path.Combine(path, "thumbnail.png"), defaultThumbnail.EncodeToPNG());

    //    return path;
    //}
    public void DeleteLocalPanorama(string path)
    {
        if (!Directory.Exists(path)) return;
        Directory.Delete(path, true);
    }
    public IEnumerator Upload(string url, string name)
    {
        url += "/Content";
        string fileName = name + ".zip";
        string zipPath = Path.Combine(Application.temporaryCachePath, fileName);

        ZipFile.CreateFromDirectory(Path.Combine(persistentDataPath, name), zipPath);
        List<IMultipartFormSection> formData = new()
        {
            new MultipartFormFileSection("file", File.ReadAllBytes(zipPath), fileName, "multipart/form-data")
        };
        File.Delete(zipPath);

        UnityWebRequest www = UnityWebRequest.Post(url, formData);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Form upload complete!");
        }
    }

    public static async Task PatchData(Config config, string path)
    {
        if (!config.contentData.TryAdd(path, await File.ReadAllBytesAsync(Path.Combine(config.path, path))))
        {
            Debug.LogWarning($"Patch data was already in config. Path: {path}");
        }
    }

    // https://stackoverflow.com/questions/468119/whats-the-best-way-to-calculate-the-size-of-a-directory-in-net
    public static long DirSize(DirectoryInfo d)
    {
        long size = 0;
        // Add file sizes.
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            size += fi.Length;
        }
        // Add subdirectory sizes.
        DirectoryInfo[] dis = d.GetDirectories();
        foreach (DirectoryInfo di in dis)
        {
            size += DirSize(di);
        }
        return size;
    }
    // returns the new path if directory did not already exists
    // otherwise returns null
    public static string ChangeDirName(string path, string newName)
    {
        string newPath = Path.Combine(Directory.GetParent(path).FullName, newName);
        if (Directory.Exists(newPath)) return null;

        Directory.Move(path, newPath);

        return newPath;
    }

    private bool ValidateAllPaths(JSONClasses.Config config)
    {
        try
        {
            foreach (NodeContent cat in config.AllNodeContents())
            {
                if (string.IsNullOrEmpty(cat.texture)) continue;
                var file = Directory.GetFiles(config.path, cat.texture, SearchOption.AllDirectories).FirstOrDefault();
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


    private void Start()
    {
        if (string.IsNullOrEmpty(persistentDataPath)) persistentDataPath = Application.persistentDataPath;
        tempDataPath = Path.Combine(persistentDataPath, "temp");
        Directory.CreateDirectory(tempDataPath);
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
            config.path = Path.GetFileName(lastArgs.FullPath);
            config.Construct();

            var newNames = config.TextureNames;
            // add new images
            foreach (string texName in lastLoadedConfig.TextureNames.Except(newNames))
            {
                //panoramaSphereController.ContentData.Add(texName, File.ReadAllBytes(folderpath + "/" + texName));
            }
            // remove obsolete images
            foreach (string texName in newNames.Except(lastLoadedConfig.TextureNames))
            {
                //panoramaSphereController.ContentData.Remove(texName);
            }

            //timelineController.Fill(config, true); // this will update the panoramaSphereController 
            //configable.SetConfig(config, true);
            return;
        }
        // Image change
        //if (panoramaSphereController.ContentData.ContainsKey(filename))
        //{
        //    panoramaSphereController.UpdateTexture(filename, File.ReadAllBytes(lastArgs.FullPath));
        //    return;
        //}

        Debug.Log("File changed in current panorama folder but isn't connected to panorama");
    }
    private void OnApplicationQuit()
    {
        watcher?.Dispose();
    }
}
