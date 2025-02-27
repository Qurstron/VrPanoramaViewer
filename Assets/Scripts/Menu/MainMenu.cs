using DG.Tweening;
using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using JSONClasses;
using Debug = UnityEngine.Debug;

public class MainMenu : MonoBehaviour
{
    public FileManager fileManager;
    public GameObject content;
    public TMP_InputField serverInput;
    public string defaultServer = "http://localhost:7206";
    [SerializeField] private View loadingView;
    [SerializeField] private View editView;
    [SerializeField] private int minMsLoading = 1000;
    [Header("Refs")]
    [SerializeField] private InfoPanel infoPanel;
    [SerializeField] private Image reloadImage;
    [Header("Prefabs")]
    [SerializeField] private GameObject menuEntryPrefab;
    [SerializeField] private GameObject menuEntryDynamicPrefab;
    [SerializeField] private GameObject menuEntry404Prefab;

    private Transform recentContent = null;
    private Transform viewerContent = null;
    private MenuEntry lastOpened = null;
    private bool loadingWasAborted = true;
    private bool wasInitalised = false;

    private void Start()
    {
        fileManager.persistentDataPath = AppConfig.Config.viewerPath;
        serverInput.text = defaultServer;
        recentContent = content.transform.GetChild(0);
        viewerContent = content.transform.GetChild(1);

        ReloadNoAnim();

        GetComponent<View>().OnShow.AddListener(async () =>
        {
            if (lastOpened == null) return;
            if (loadingWasAborted)
            {
                loadingWasAborted = false;
                return;
            }

            string path = lastOpened.PanoramaMenuEntry.path;
            var data = await fileManager.GetLocalPanorama(path);
            StartCoroutine(fileManager.GetLocalThumbnail(path, tex =>
            {
                data.thumbnail = tex;
                lastOpened.SetData(data, str => { });
            }));
        });

        string path = GetArg("-open");
        if (!string.IsNullOrEmpty(path))
        {
            OpenPanorama(path);
        }

        wasInitalised = true;
    }
    private void OnEnable()
    {
        if (!wasInitalised) return;
        ReloadNoAnim();
    }

    /// <summary>
    /// Prompts the user to select a new Viewing folder
    /// </summary>
    public void ChangeViewerFolder()
    {
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Viewer Folder", AppConfig.Config.viewerPath, false);
        if (paths.Length != 1) return;
        string path = paths[0];

        AppConfig.Config.viewerPath = path;
        AppConfig.Save();
        Reload();
    }
    /// <summary>
    /// Prompts the user to select a panorama from the explorer ans opens it.
    /// </summary>
    public async void OpenNew()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Project Config", AppConfig.Config.viewerPath, "json", false);
        if (paths.Length != 1) return;
        string path = new FileInfo(paths[0]).Directory.FullName;

        PanoramaMenuEntry entry = await fileManager.GetLocalPanorama(path);

        if (entry == null) return;
        if (!AppConfig.Config.recentProjects.Exists(p => p.path == path))
        {
            AppConfig.Config.recentProjects.Add(new(path, DateTime.Now));
            AppConfig.Save();
            ReloadNoAnim();

            Debug.Log($"{path} added");
        }

        OpenPanorama(entry.path);
    }
    //public async void CreatePanorama()
    //{
    //    //string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Project Location", AppConfig.Config.viewerPath, false);
    //    //if (paths.Length != 1) return;
    //    //string path = paths[0];

    //    //path = await fileManager.CreateLocalPanorama(path, "New Project");
    //    //AppConfig.Config.recentProjects.Add(path);
    //    //AppConfig.Save();

    //    //Reload();
    //}
    public void DownloadNew()
    {
        throw new Exception("not implemented");
    }
    /// <returns>True if panorama was opened correctly</returns>
    public async Task<bool> OpenPanorama(MenuEntry entry)
    {
        loadingView.transform.Find("Header").GetComponent<TMP_Text>().text = $"Loading {entry.PanoramaMenuEntry.config.name} ...";
        bool success = await OpenPanorama(entry.PanoramaMenuEntry.path);

        if (!success)
        {
            entry.PanoramaMenuEntry.error = fileManager.LastLoadError;
            if (entry.PanoramaMenuEntry.error == PanoramaMenuEntry.Error.Undefined)
                entry.PanoramaMenuEntry.customError = fileManager.LastLoadMsg;
            entry.UpdateData();
            Button button = entry.GetComponent<Button>();
        }
        else entry.ClearLoadError();

        return success;
    }
    /// <returns>True if panorama was opened correctly</returns>
    public async Task<bool> OpenPanorama(string path)
    {
        loadingView.Display();
        Stopwatch stopWatch = new();
        stopWatch.Start();

        Config config = await fileManager.LoadLocalPanorama(path);

        stopWatch.Stop();
        int delay = Math.Max(minMsLoading - (int)stopWatch.Elapsed.TotalMilliseconds, 0);
        await Task.Delay(delay);

        if (config == null)
        {
            loadingView.UnDisplay();
            loadingWasAborted = true;
            return false;
        }

        AppConfig.UpdateRecentPath(path);
        editView.GetComponentInChildren<ProjectEditor>().SetConfig(config);
        editView.Display();
        return true;
    }
    //public bool OpenPanorama(Config config)
    //{
    //    loadingView.transform.Find("Header").GetComponent<TMP_Text>().text = $"Loading {config.name} ...";
    //    if (!loadingView.IsTop)
    //        loadingView.Display();

    //    if (config == null)
    //    {
    //        loadingView.UnDisplay();
    //        loadingWasAborted = true;
    //    }

    //    AppConfig.Config.recentProjects.FirstOrDefault(p => p.path == config.path).lastAccess = DateTime.Now;
    //    editView.GetComponentInChildren<ProjectEditor>().SetConfig(config);
    //    editView.Display();
    //}

    /// <summary>
    /// Completly reloads all recent and viewer panoramas
    /// </summary>
    public async void Reload()
    {
        reloadImage.transform.DORotate(new(0, 0, -360), .25f, RotateMode.FastBeyond360).SetEase(Ease.OutSine);
        await ReloadNoAnim();
    }
    public async Task ReloadNoAnim()
    {
        int j = 0;
        foreach (Transform child in viewerContent.GetChild(1))
        {
            if (j == viewerContent.GetChild(1).childCount - 1) continue;
            Destroy(child.gameObject);
            j++;
        }
        j = 0;
        foreach (Transform child in recentContent.GetChild(1))
        {
            if (j == recentContent.GetChild(1).childCount - 1) continue;
            Destroy(child.gameObject);
            j++;
        }

        List<PanoramaMenuEntry> localEntries = new();
        if (!string.IsNullOrEmpty(fileManager.persistentDataPath))
        {
            localEntries = await fileManager.GetLocalPanoramasInDirectory(AppConfig.Config.viewerPath, true);
            FillList(viewerContent, localEntries);
        }

        List<PanoramaMenuEntry> entries = new();
        foreach (var recentProject in AppConfig.Config.recentProjects)
        {
            string file = recentProject.path;
            if (Directory.GetParent(file).FullName.Equals(fileManager.persistentDataPath))
            {
                var e = localEntries.Find(a => a.path.Equals(file));
                if (e != null)
                {
                    e.recentProject = recentProject;
                    entries.Add(e);
                    continue;
                }
            }

            PanoramaMenuEntry entry = await fileManager.GetLocalPanorama(file);
            if (entry != null) entries.Add(entry);
        }
        FillList(recentContent, entries, true);
    }
    private void CreateButton(Transform parent, PanoramaMenuEntry panorama, bool isDynamic = false)
    {
        if (panorama.error == PanoramaMenuEntry.Error.FileNotFound)
        {
            // may be create a placeholder for the missing file
            return;
        }

        GameObject go = Instantiate(isDynamic ? menuEntryDynamicPrefab : menuEntryPrefab, parent);
        MenuEntry entry = go.GetComponent<MenuEntry>();

        go.GetComponent<MenuEntry>().SetData(panorama, newName =>
        {
            string newPath = FileManager.ChangeDirName(panorama.path, newName);
            if (string.IsNullOrEmpty(newPath))
            {
                Debug.LogError($"name {newName} is already taken in that directory");
                entry.UpdateData();
                return;
            }

            panorama.config.name = newName;
            panorama.path = newPath;
            entry.UpdateData();

            //Sort(parent, isDynamic);
        });
        StartCoroutine(fileManager.GetLocalThumbnail(panorama.path, tex =>
        {
            panorama.thumbnail = tex;
            entry.UpdateData();
        }));
        // main button used to select
        go.GetComponent<Button>().onClick.AddListener(() =>
        {
            lastOpened = entry;
            OpenPanorama(entry);
        });

        // bottom right buttons
        Button[] buttons = go.GetComponentsInChildren<Button>();
        // info
        buttons[1].onClick.AddListener(() =>
        {
            infoPanel.DisplayInfo(panorama);
        });
        // upload
        buttons[2].onClick.AddListener(() =>
        {
            StartCoroutine(fileManager.Upload(serverInput.text, panorama.config.name));
        });
        // open folder location in explorer
        buttons[3].onClick.AddListener(() =>
        {
            Process.Start(panorama.path);
        });
        // delete
        buttons[4].onClick.AddListener(() =>
        {
            fileManager.DeleteLocalPanorama(panorama.path);
            if (AppConfig.Config.recentProjects.Exists(p => p.path == panorama.path))
            {
                AppConfig.Config.recentProjects.RemoveAll(p => p.path == panorama.path);
                AppConfig.Save();
            }

            Destroy(go);
            StartCoroutine(UpdateLayoutGroup(parent.parent));
        });

        if (isDynamic)
        {
            // remove from recent
            buttons[5].onClick.AddListener(() =>
            {
                AppConfig.Config.recentProjects.RemoveAll(p => p.path == panorama.path);
                AppConfig.Save();
                Destroy(go);
                StartCoroutine(UpdateLayoutGroup(parent.parent));
            });
        }
    }
    private void FillList(Transform parent, List<PanoramaMenuEntry> panoramaEntries, bool isDynamic = false)
    {
        parent = parent.GetChild(1);
        if (panoramaEntries.Count > 0) parent.GetChild(0).gameObject.SetActive(false);

        foreach (PanoramaMenuEntry panorama in panoramaEntries)
        {
            CreateButton(parent, panorama, isDynamic);
        }

        //Sort(parent, isDynamic);
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }
    //private void Sort(Transform parent, bool orderByDate = false)
    //{
    //    var entries = parent.GetComponentsInChildren<MenuEntry>();
    //    if (orderByDate) entries = entries.OrderByDescending(entry => entry.PanoramaMenuEntry.lastEdited).ToArray();
    //    else entries = entries.OrderBy(entry => entry.PanoramaMenuEntry.config.name).ToArray();

    //    for (int i = 0; i < entries.Length; i++)
    //    {
    //        entries[i].transform.SetSiblingIndex(i);
    //    }

    //    StartCoroutine(UpdateLayoutGroup(parent.parent));
    //}

    private IEnumerator UpdateLayoutGroup(Transform transform)
    {
        yield return new WaitForEndOfFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
    }
    private bool ComaprePath(string path1, string path2)
    {
        return 0 == String.Compare(
            Path.GetFullPath(path1).TrimEnd('\\'),
            Path.GetFullPath(path2).TrimEnd('\\'),
            StringComparison.InvariantCultureIgnoreCase);
    }
    // https://stackoverflow.com/questions/39843039/game-development-how-could-i-pass-command-line-arguments-to-a-unity-standalo
    private static string GetArg(string name)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == name && args.Length > i + 1)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
