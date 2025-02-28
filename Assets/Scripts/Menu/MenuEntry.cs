using JSONClasses;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Image = UnityEngine.UI.Image;

public class MenuEntry : MonoBehaviour
{
    [SerializeField] private GameObject errorTarget;
    [SerializeField] private Sprite errorImage;

    public PanoramaMenuEntry PanoramaMenuEntry { get; private set; }

    public void SetData(PanoramaMenuEntry entry, UnityAction<string> nameCallback)
    {
        PanoramaMenuEntry = entry;

        //TMP_InputField nameField = transform.Find("Padding/Name InputField (TMP)").GetComponent<TMP_InputField>();
        //UnityAction<string> listener = newName =>
        //{
        //    if (PanoramaMenuEntry.name.Equals(newName)) return;

        //    //PanoramaMenuEntry.name = newName;
        //    nameCallback.Invoke(newName);
        //    //UpdateData();

        //    Debug.Log($"{entry.name} changed to {newName}");
        //};

        //nameField.onSubmit.AddListener(listener);
        //nameField.onDeselect.AddListener(listener);

        UpdateData();
    }
    public void UpdateData()
    {
        TMP_Text title = transform.Find("Padding/Title").GetComponent<TMP_Text>();
        TMP_Text timeDisplay = transform.Find("Padding/Time").GetComponent<TMP_Text>();
        Transform pathTransform = transform.Find("Padding/Path");

        SetThumbnail(PanoramaMenuEntry.thumbnail);

        if (PanoramaMenuEntry.recentProject != null)
        {
            timeDisplay.text = PanoramaMenuEntry.recentProject.lastAccess.ToString(CultureInfo.CurrentCulture);
        }
        else
        {
            timeDisplay.text = PanoramaMenuEntry.lastEdited.ToString(CultureInfo.CurrentCulture);
        }

        if (string.IsNullOrEmpty(PanoramaMenuEntry.config.name))
        {
            title.text = "* Missing Title *";
            title.fontStyle = FontStyles.Italic;
        }
        else
        {
            title.text = PanoramaMenuEntry.config.name;
            title.fontStyle = FontStyles.Normal;
        }

        if (pathTransform != null)
        {
            pathTransform.GetComponent<TMP_Text>().text = PanoramaMenuEntry.path;
        }

        switch (PanoramaMenuEntry.error)
        {
            case PanoramaMenuEntry.Error.NoError:
                ClearLoadError();
                break;
            case PanoramaMenuEntry.Error.FileNotFound:
                DisplayLoadError("File not found");
                break;
            case PanoramaMenuEntry.Error.Deserialize:
                DisplayLoadError("Couldn't be deserialized");
                break;
            case PanoramaMenuEntry.Error.Validation:
                DisplayLoadError("Couldn't be validated. See info for a list of errors");
                break;
            default:
                DisplayLoadError("Something went wrong. See info for a list of errors");
                break;
        }
    }
    public void DisplayLoadError(string msg)
    {
        if (string.IsNullOrEmpty(msg))
        {
            ClearLoadError();
            return;
        }

        errorTarget.SetActive(true);
        errorTarget.GetComponentInChildren<TMP_Text>().text = msg;
    }
    public void ClearLoadError()
    {
        errorTarget.SetActive(false);
    }

    private void SetThumbnail(Texture2D tex)
    {
        if (tex == null) SetThumbnail(errorImage);
        else SetThumbnail(Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f));
    }
    private void SetThumbnail(Sprite sprite)
    {
        transform.Find("Padding/Envelope/Thumbnail").GetComponent<Image>().sprite = sprite;
    }
    private void Start()
    {
        if (errorTarget == null)
        {
            errorTarget = transform.Find("Padding/ErrorDisplay").gameObject;
        }
        errorTarget.SetActive(false);
    }
}
