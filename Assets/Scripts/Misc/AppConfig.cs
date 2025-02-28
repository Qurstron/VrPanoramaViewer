using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This config is for the application and not to be confused with the config for a project.
/// This is effectively a singleton.
/// </summary>
public class AppConfig : MonoBehaviour
{
    [SerializeField] private CanvasScaler scaler;
    [SerializeField] private string path; // this is basically the same as configPath, but Unity can't serialize a static field
    [SerializeField] private bool saveOnQuit = true;
    private static string configPath;
    public static ConfigObject Config { get; private set; } = null;

    public static async void Save()
    {
        Config.recentProjects.Sort((a, b) => -DateTime.Compare(a.lastAccess, b.lastAccess));
        await File.WriteAllTextAsync(configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
    }
    /// <summary>
    /// Updates an existing entry in recentProjects or adds a new one.
    /// </summary>
    public static void UpdateRecentPath(string path)
    {
        RecentProject rp = Config.recentProjects.Find(x => x.path == path);

        if (rp == null)
        {
            rp = new()
            {
                path = path
            };
            Config.recentProjects.Add(rp);
        }
        rp.lastAccess = DateTime.Now;

        Save();
    }

    // awake is used instead of Start, because Awake is called before Start
    // Other Components that want to use the config can do so in Start on application execution or later
    private void Awake()
    {
        if (Config != null) throw new Exception("AppConfig is already constructed");
        if (string.IsNullOrEmpty(path)) configPath = Path.Combine(Application.dataPath, "config.json");
        else configPath = path;

        try
        {
            if (File.Exists(configPath))
            {
                Config = JsonConvert.DeserializeObject<ConfigObject>(File.ReadAllText(configPath));
            }
            else
            {
                File.Create(configPath);
            }
        }
        catch (Exception e)
        {
            if (e is JsonException)
            {
                Debug.LogError("Unable to read or deserialize config, delete old Config");
                File.Delete(configPath);
            }
            else
            {
                Debug.LogError("Unknown error while loading config");
            }
        }
        finally
        {
            Config ??= new ConfigObject();
        }

        Setup();
    }
    private void OnApplicationQuit()
    {
        if (saveOnQuit) Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        Config = null;
    }
#if UNITY_EDITOR
    private static void OnExitPlayMode(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Config = null;
        }
    }
#endif

    /// <summary>
    /// Gets called after config is loaded. Sets the global application data to the config data
    /// </summary>
    private void Setup()
    {
        // Try and guess the viewer path if not already set
        if (string.IsNullOrEmpty(Config.viewerPath))
        {
            string tempConfigPath = Path.Combine(Directory.GetParent(Application.persistentDataPath).FullName, "VrPanorama");
            if (Directory.Exists(tempConfigPath))
                Config.viewerPath = tempConfigPath;
        }

        Application.targetFrameRate = Config.maxFPS;
        QualitySettings.vSyncCount = Config.vsync;
        if (scaler == null)
            Debug.Log("No CanvasScaler specified. UIScaling is disabled.");
        else
            scaler.referenceResolution = new Vector2(1920, 1080) / Config.uiScaling;

        Invoke(nameof(DelayedSetup), 1);
    }
    private void DelayedSetup()
    {
        Application.runInBackground = Config.runInBackground;
    }

    /// <remarks>
    /// Changes to the config at runtime that require setup may not be effective until restart!
    /// </remarks>
    [Serializable]
    public class ConfigObject
    {
        public string viewerPath;
        public List<RecentProject> recentProjects = new();

        public int maxFPS = 60; // https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html
        public int vsync = 1; // https://docs.unity3d.com/ScriptReference/QualitySettings-vSyncCount.html

        public float uiScaling = 1;
        public float selectPulseTime = 1;
        public float defaultOutlineSize = 5;
        public string outlineColor = "#FF6600";
        public string outlinePulseColor = "#000000";
        public bool runInBackground = false;
        [JsonIgnore] public Color OutlineColor
        {
            get { return QUtils.StringToColor(outlineColor); }
        }
        [JsonIgnore] public Color OutlinePulseColor
        {
            get { return QUtils.StringToColor(outlinePulseColor); }
        }

        public uint maxNodesForceDirectedPerUpdate = 50;
    }

    [Serializable]
    public class RecentProject
    {
        public string path;
        public DateTime lastAccess = DateTime.Now;

        public RecentProject() { }
        public RecentProject(string path, DateTime lastAccess)
        {
            this.path = path;
            this.lastAccess = lastAccess;
        }

        public override bool Equals(object obj)
        {
            return obj is RecentProject project &&
                   path == project.path;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(path);
        }
    }
}
