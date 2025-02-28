using JSONClasses;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class FloppyDisk : MonoBehaviour
{
    public XRGrabInteractable grabInteractable;

    private string panoramaName;
    public string PanoramaName
    {
        get { return panoramaName; }
        set
        {
            panoramaName = value;
            transform.Find("Canvas/Text").GetComponent<TMP_Text>().text = panoramaName;
        }
    }

    public PanoramaMenuEntry entry;

    public void SetThumbnail(Sprite sprite)
    {
        transform.Find("Canvas/Image").GetComponent<Image>().sprite = sprite;
    }

    // Start is called before the first frame update
    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }
}
