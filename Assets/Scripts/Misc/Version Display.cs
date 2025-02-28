using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class VersionDisplay : MonoBehaviour
{
    public string suffix = "Version: ";

    void Start()
    {
        GetComponent<TMP_Text>().text = suffix + Application.version;

    }
    private void OnValidate()
    {
        Start();
    }
}
