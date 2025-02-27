using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SelectFileButtonExtension : MonoBehaviour
{
    [Tooltip("Can be null")]
    [SerializeField] private TMP_InputField relatedInputField;
    [SerializeField] private string title = "Open File";
    [Tooltip("Purly cosmetic and has no impact on file extensions")]
    [SerializeField] private string fileType = "All Files";
    [Tooltip("Multiple extensions are divided by \".\"")]
    [SerializeField] private string extensions = "";
    [SerializeField] private bool isOpenFolder = false;
    [SerializeField] private bool isMultiSelect = false;
    //[Tooltip("Interprets the text in relatedInputField as a path and checks if it is ")]
    //[SerializeField] private bool validateRelatedInputField = true;
    [Tooltip("The location that the explorer opens")]
    public string defaultPath;
    [Tooltip("Only gets called when the path array is not null or empty")]
    public UnityEvent<string[]> onValueChange;

    private string[] paths;
    public string[] Paths { get { return paths; } }
    public bool HasRecievedUserInput { get; private set; } = false;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            ExtensionFilter[] filters = new ExtensionFilter[1];
            string[] pathsBuffer;

            if (string.IsNullOrEmpty(title)) title = "Open File";
            if (string.IsNullOrEmpty(fileType)) fileType = "Files";
            if (string.IsNullOrEmpty(extensions)) extensions = "*";
            filters[0] = new(fileType, extensions.Split('.'));

            if (isOpenFolder) pathsBuffer = StandaloneFileBrowser.OpenFolderPanel(title, defaultPath, isMultiSelect);
            else pathsBuffer = StandaloneFileBrowser.OpenFilePanel(title, defaultPath, filters, isMultiSelect);
            if (pathsBuffer.Length < 1) return;

            HasRecievedUserInput = true;
            paths = pathsBuffer;
            onValueChange.Invoke(paths);
        });

        if (relatedInputField != null)
        {
            onValueChange.AddListener(paths =>
            {
                relatedInputField.text = paths[0];
                relatedInputField.onSubmit.Invoke(paths[0]);
            });
        }
    }
}
