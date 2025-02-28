using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TMPro;
using UnityEngine;
using JSONClasses;
using static UnityEngine.InputSystem.InputAction;
using Application = UnityEngine.Application;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.UI;
using SFB;
using System.IO;

//[RequireComponent(typeof(View))]
public class ProjectEditor : Configable
{
    [Header("References")]
    [SerializeField] private FileManager fileManager;
    [SerializeField] private PanoramaSphereController sphereController;
    [SerializeField] private CameraHandler cameraHandler;
    [SerializeField] private Transform cradTarget;
    [SerializeField] private TabMenu tabs;
    [SerializeField] private TitleField titleField;
    [SerializeField] private StateControler stateControler;
    [Header("Prefabs")]
    [SerializeField] private GameObject highlightPrefab;
    [SerializeField] private GameObject labelCardPrefab;
    [SerializeField] private GameObject lineCardPrefab;

    public bool IsHighlightDirty { get; private set; } = false;
    public bool IsDirty
    {
        get { return commandExecutionsSinceSave.Any(command => command is IDirtyCommand); }
    }

    private readonly Stack<ICommand> commandHistory = new();
    private readonly Stack<ICommand> redoCommandHistory = new();
    private readonly HashSet<ICommand> commandExecutionsSinceSave = new();
    private MultiCommand multiCommand;

    /// <summary>
    /// Contains all the names of the commands with the execution type and other control events
    /// </summary>
    private readonly List<string> completeHistory = new();
    private readonly List<GameObject> highlightGOs = new();
    private ProjectContext context;
    private GameObject currentUniqueCrad;
    private string titel;
    private static bool overrideQuit = false;
    private static ProjectEditor editor;

    public override void SetConfig(Config config, bool tryKeepIndices = false)
    {
        Config = config;

        commandHistory.Clear();
        redoCommandHistory.Clear();
        commandExecutionsSinceSave.Clear();
        completeHistory.Clear();

        context = new ProjectContext(this, Config)
        {
            currentNode = Config.rootNode,
            currentNodeContent = Config.rootNode.content[0]
        };
        try
        {
            SendContext(transform);
            stateControler.ToggleMenu();
            // PAIN
            //sphereController.Context = context;

            context.OnConfigChange.Invoke();
            context.OnCategoryNameChange.Invoke();
            context.OnSelectionChange.Invoke();

        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        QUtils.ChangeWindowTitle($"{Config.name} - {titel}");
    }
    public void BackToMenu()
    {
        if (IsDirty)
        {
            DisplayQuitModal();
            return;
        }

        QuitToMenu();
    }
    private static void DisplayQuitModal(bool isQuitting = false)
    {
        Action quit = isQuitting ? QuitToDesktop : QuitToMenu;
        Modal.Choice[] choices =
        {
            new ("Save", () =>
            {
                editor.SaveProject();
                quit();
            }),
            new ("Dont't Save", () => quit()),
            new ("Cancel", () => { }),
        };
        Modal.SetModal("Unsaved changes", "", choices);
    }
    private static void QuitToMenu()
    {
        AppConfig.UpdateRecentPath(editor.Config.path);
        
        editor.GetComponent<View>().ForceUnDisplay();
        editor.SetUniqueCard(null);
    }
    private static void QuitToDesktop()
    {
        AppConfig.Config.recentProjects.FirstOrDefault(p => p.path == editor.Config.path).lastAccess = DateTime.Now;

        overrideQuit = true;
        Application.Quit();
    }
    private static bool WantsToQuit()
    {
        if (editor == null) return true;

        // Don't get in the way if the editor isn't opend
        // can cause problems if the view is still contained in the viewstack
        // fix: check view stack
        //if (!editor.GetComponent<View>().IsInViewStack || overrideQuit) return true;
        //if (editor.IsDirty)
        //{
        //    DisplayQuitModal(true);
        //    return false;
        //}
        return true;
    }
    [RuntimeInitializeOnLoadMethod]
    static void RunOnStart()
    {
        Application.wantsToQuit += WantsToQuit;
    }

    public void ExecuteCommand(ICommand command)
    {
        try
        {
            if (command.Execute(context))
            {
                // redo histroy is cleared because we are now on an different timeline
                redoCommandHistory.Clear();
                commandHistory.Push(command);

                AddCommandSinceSave(command);

                LogHistory($"executed {command}");
                return;
            }
            LogHistory($"refused {command}");
        }
        catch (Exception e)
        {
            // reflect e.Message back to user
            Debug.LogException(e);
            Debug.LogError(e.ToString());
        }
    }
    /// <summary>
    /// Joins the command with the currently executed command.
    /// Only works if a command is currently executed.
    /// </summary>
    public void AppendCommand(ICommand command)
    {
        if (multiCommand == null)
        {
            MultiCommand multiCommand = new("internal");
        }
        multiCommand.AppendCommand(command);
    }
    public void Undo()
    {
        Undo(new());
    }
    public void Redo()
    {
        Redo(new());
    }
    public void Undo(CallbackContext callbackContext)
    {
        if (!CheckHotkeyContext(callbackContext)) return;
        if (commandHistory.Count <= 0) return;

        ICommand command = commandHistory.Pop();
        redoCommandHistory.Push(command);
        command.Undo(context);

        AddCommandSinceSave(command);

        LogHistory($"undo {command}");
    }
    public void Redo(CallbackContext callbackContext)
    {
        if (!CheckHotkeyContext(callbackContext)) return;
        if (redoCommandHistory.Count <= 0) return;

        ICommand command = redoCommandHistory.Pop();
        commandHistory.Push(command);
        command.Execute(context);

        AddCommandSinceSave(command);

        LogHistory($"redo {command}");
    }
    private void AddCommandSinceSave(ICommand command)
    {
        if (commandExecutionsSinceSave.Contains(command)) commandExecutionsSinceSave.Remove(command);
        else commandExecutionsSinceSave.Add(command);
    }

    // Menubar functions
    public async void SaveProject()
    {
        await fileManager.SaveLocalPanorama(context.Config);
        commandExecutionsSinceSave.Clear();
        LogHistory("Save Project");
    }
    public void SaveProject(CallbackContext callbackContext)
    {
        if (!CheckHotkeyContext(callbackContext)) return;
        SaveProject();
    }
    public void ShowProjectInExplorer()
    {
        Process.Start(context.Config.path);
    }
    public void ChangeThumbnail()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Thumbnail", Config.path, "png", false);
        if (paths.Length != 1) return;

        File.Copy(paths[0], Path.Combine(Config.path, "thumbnail.png"), true);
    }
    public async void ExportToZip()
    {
        string path = StandaloneFileBrowser.SaveFilePanel("Export Project", fileManager.persistentDataPath, Config.name, "zip");
        if (string.IsNullOrEmpty(path)) return;

        await fileManager.ExportLocalPanorama(Config, path, true);
    }
    public void ExportToServer()
    {

    }
    public async void Import3DObject()
    {
        ExtensionFilter[] filters = { new("glTF", "glb", "gltf") };
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Select Thumbnail", Config.path, filters, false);
        if (paths.Length != 1) return;
        string path = paths[0];
        string localPath;

        if (QUtils.SrcPathContainsDestPath(path, Config.path))
        {
            localPath = Path.GetRelativePath(Config.path, path);
        }
        else
        {
            localPath = Path.Combine("3DObjects", Path.GetFileName(path));
            string fullPath = Path.Combine(Config.path, localPath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            File.Copy(path, fullPath);
        }

        Object3D object3D = new()
        {
            file = localPath,
            name = "New Object",
            origin = context.currentNodeContent
        };
        await FileManager.PatchData(Config, localPath);

        var objComponent = (await sphereController.LoadGltfBinaryFromMemory(object3D)).GetComponent<SceneRootContainer>().selectable;
        ExecuteCommand(new DeleteWorldObjectCommand(objComponent.GetPoints(), false));
    }

    public void FocusSelection()
    {
        FocusSelection(new());
    }
    public void FocusSelection(CallbackContext callbackContext)
    {
        //if (!CheckHotkeyContext(callbackContext)) return;

        //if (context.selectedAngles.Count > 0)
        //{
        //    cameraHandler.FocusAngel(context.AvgSelectedAngles);
        //}
        //if (context.selectedObjects.Count > 0)
        //{
        //    cameraHandler.FocusPosition(context.AvgSelectedObjectPos);
        //}
    }
    public void DeleteSelection(CallbackContext callbackContext)
    {
        if (!CheckHotkeyContext(callbackContext)) return;

        context.selectedAngles.Select(angle => angle.relatedComponent);
        ExecuteCommand(new DeleteWorldObjectCommand(context.selectedAngles, true));
    }

    /// <param name="selectable">Selectable displayed in the Card, null to clear Card</param>
    public void SetUniqueCard(AngleSelectable selectable)
    {
        //if (currentUniqueCrad != null) Destroy(currentUniqueCrad);
        //if (selectable == null) return;

        //GameObject card = null;
        //switch (selectable)
        //{
        //    case LabelComponent labelComponent:
        //        context.currentLabel = labelComponent;
        //        card = Instantiate(labelCardPrefab, cradTarget);
        //        break;

        //    case LineComponent lineComponent:
        //        context.currentLine = lineComponent;
        //        card = Instantiate(lineCardPrefab, cradTarget);
        //        break;

        //    default:
        //        throw new Exception("Unsupported selectable type");
        //}

        //currentUniqueCrad = card;
        //SendContext(card.transform);
        //context.OnObjectChange.Invoke();
    }
    private void UpdateHighlights()
    {
        //foreach (var highlight in highlightGOs)
        //{
        //    Vector2 angle = highlight.GetComponent<Highlight>().virtualParent.Angle;
        //    Vector3 pos = PanoramaSphereController.convertArrayToPos(angle);
        //    highlight.transform.position = cameraHandler.ConvertWorldToUI(pos);
        //}
    }
    private void SetHighlights()
    {
        //foreach (GameObject highlightGO in highlightGOs)
        //{
        //    Destroy(highlightGO);
        //}
        //highlightGOs.Clear();

        //foreach (AnglePoint point in context.selectedAngles)
        //{
        //    GameObject highlight = Instantiate(highlightPrefab, cameraHandler.highlightTarget);
        //    (highlight.AddComponent(typeof(Highlight)) as Highlight).virtualParent = point;
        //    highlight.transform.localScale = new(.5f, .5f, 1);
        //    highlightGOs.Add(highlight);
        //}
    }

    public void SetSelectionDirty()
    {
        if (!IsHighlightDirty)
        {
            SetHighlights();
            IsHighlightDirty = true;
        }
    }

    /// <summary>
    /// Gets a history of all executed commands in a string array
    /// use JumpToCommand to revert to a specific command
    /// </summary>
    /// <returns>The names of the executed commands</returns>
    public string[] GetHistory()
    {
        return commandHistory.Select(cmd => cmd.GetType().Name).ToArray();
    }
    /// <summary>
    /// Reverts to a specific point by undoing all previous steps to that point.
    /// Use GetHistory() to find commands.
    /// </summary>
    /// <param name="index">Index of the latest executed command to jump to</param>
    public void JumpToCommand(int index)
    {
        if (commandHistory.Count <= index) return;

        LogHistory($"Jump to command {index} ({commandHistory.Count - index} undos)");
        while (commandHistory.Count > index)
        {
            Undo();
        }
    }
    private void LogHistory(string str)
    {
        Debug.Log(str);
        completeHistory.Add(str);
    }

    /// <summary>
    /// We need to wait for a frame to set the context of the ConfigActors,
    /// since view was just enabled and the inputfield gets confused and only shows one character.
    /// This also leads to other failures.
    /// The 1 frame delay isn't noticable and is covered by the loading view anyway
    /// </summary>
    //private IEnumerator SendConfig()
    //{
    //    yield return new WaitForEndOfFrame();

    //    SendContext(transform);
    //    sphereController.Context = context;

    //    context.OnConfigChange.Invoke();
    //    context.OnCategoryNameChange.Invoke();
    //}
    private void SendContext(Transform root)
    {
        foreach (ConfigActor actor in root.GetComponentsInChildren<ConfigActor>(true))
        {
            actor.Context = context;
        }
    }
    private bool CheckHotkeyContext(CallbackContext? context)
    {
        if (context == null) return true;
        return ((CallbackContext)context).performed && GetComponent<View>().IsTop;
    }

    private void Awake()
    {
        editor = this;
        titel = Application.productName;

        //titleField.onFieldChange.AddListener(str =>
        //{
        //    string title = $"{str} - {titel}";
        //    if (IsDirty) title = "* " + title;
        //    QUtils.ChangeWindowTitle(title);
        //});
    }
    private void Update()
    {
        UpdateHighlights();
        if (IsHighlightDirty) IsHighlightDirty = false;
    }
    private void OnDisable()
    {
        QUtils.ChangeWindowTitle(titel);
    }
}
