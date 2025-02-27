using JSONClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent (typeof(View))]
public class CreateNewProject : MonoBehaviour
{
    [SerializeField] private View loadingView;
    [SerializeField] private View editView;
    [SerializeField] private FileManager fileManager;
    [SerializeField] private Transform listTransform;
    [SerializeField] private GameObject listGameObject;
    [SerializeField] private List<Template> templates;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField pathInputField;
    [SerializeField] private SelectFileButtonExtension selectFileButton;
    [SerializeField] private Image thumbnail;
    [SerializeField] private TMP_Text header;
    [SerializeField] private TMP_Text content;
    [SerializeField] private Image preview;
    [SerializeField] private Button submit;
    private Template currentTemplate;

    public void InitCreate()
    {
        thumbnail.enabled = false;
        foreach (var errorDisplay in GetComponentsInChildren<InputErrorDisplay>())
        {
            errorDisplay.Clear();
        }

        listTransform.GetChild(0).GetComponentInChildren<Toggle>().isOn = true;

        nameInputField.text = "New Project";
        pathInputField.text = AppConfig.Config.viewerPath;
        GetComponent<View>().Display();
    }

    public async void Create()
    {
        if (!checkInputs()) return;
        loadingView.Display();

        try
        {
            string thumbnailPath = selectFileButton.HasRecievedUserInput ? selectFileButton.Paths[0] : null;
            Config config = await fileManager.CreateLocalPanorama(currentTemplate, nameInputField.text, pathInputField.text, thumbnailPath);
            //menu.OpenPanorama(config);
            AppConfig.Config.recentProjects.Add(new(config.path, DateTime.Now));
            AppConfig.Save();
            editView.GetComponentInChildren<ProjectEditor>().SetConfig(config);
            editView.Display();
        }
        catch (Exception e)
        {
            loadingView.UnDisplay();
            // TODO: display error
        }
        finally
        {
            Application.runInBackground = AppConfig.Config.runInBackground;
        }
    }

    private void Awake()
    {
        ToggleGroup toggleGroup = listTransform.GetComponent<ToggleGroup>();

        foreach (Transform child in listTransform)
        {
            if (listGameObject.transform == child) continue;
            Destroy(child.gameObject);
        }
        foreach (var template in templates)
        {
            GameObject entry = Instantiate(listGameObject, listTransform);
            Toggle toggle = entry.GetComponent<Toggle>();
            entry.name = template.name;
            entry.GetComponentInChildren<TMP_Text>().text = template.name;
            entry.SetActive(true);
            toggle.isOn = false;
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (!isOn) return;
                SetTemplate(template);
            });
            toggle.group = toggleGroup;
        }
        listGameObject.SetActive(false);
        listGameObject.transform.SetAsLastSibling();
    }
    private void Start()
    {        
        selectFileButton.onValueChange.AddListener(paths =>
        {
            try
            {
                Texture2D tex = new(1, 1);
                tex.LoadImage(File.ReadAllBytes(paths[0]));
                thumbnail.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width / 2, tex.height / 2));
                thumbnail.enabled = true;
            }
            catch
            {
                // TODO: Display error
            }
        });

        nameInputField.onEndEdit.AddListener(str => checkInputs());
        pathInputField.onEndEdit.AddListener(str => checkInputs());
        //nameInputField.onEndEdit.AddListener(name =>
        //{
        //    if (string.IsNullOrWhiteSpace(name))
        //        QUtils.GetOrAddComponent<InputErrorDisplay>(nameInputField).DisplayError("Name can not be empty");
        //});
        //pathInputField.onEndEdit.AddListener(path =>
        //{
        //    if (!Directory.Exists(path))
        //    {
        //        QUtils.GetOrAddComponent<InputErrorDisplay>(pathInputField).DisplayError("Directory dosn't exists");
        //        return;
        //    }
        //    if (!IsDirectoryWritable(path))
        //    {
        //        QUtils.GetOrAddComponent<InputErrorDisplay>(pathInputField).DisplayError("The user dosn't have wirting rights in the directory");
        //        return;
        //    }
        //});
    }
    private void SetTemplate(Template template)
    {
        currentTemplate = template;
        header.text = template.projectName;
        content.text = template.description;
        preview.sprite = template.preview;
    }

    // https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
    private bool IsDirectoryWritable(string dirPath, bool throwIfFails = false)
    {
        try
        {
            using (FileStream fs = File.Create(
                Path.Combine(
                    dirPath,
                    Path.GetRandomFileName()
                ),
                1,
                FileOptions.DeleteOnClose)
            )
            { }
            return true;
        }
        catch
        {
            if (throwIfFails)
                throw;
            else
                return false;
        }
    }

    private bool checkInputs()
    {
        bool isValid = true;

        if (string.IsNullOrWhiteSpace(nameInputField.text))
        {
            QUtils.GetOrAddComponent<InputErrorDisplay>(nameInputField).DisplayError("Name can not be empty");
            isValid = false;
        }
        if (!Directory.Exists(pathInputField.text))
        {
            QUtils.GetOrAddComponent<InputErrorDisplay>(pathInputField).DisplayError("Directory dosn't exists");
            isValid = false;
        }
        if (!IsDirectoryWritable(pathInputField.text))
        {
            QUtils.GetOrAddComponent<InputErrorDisplay>(pathInputField).DisplayError("The user dosn't have wirting rights in the directory");
            isValid = false;
        }

        submit.interactable = isValid;

        return isValid;
    }
}
