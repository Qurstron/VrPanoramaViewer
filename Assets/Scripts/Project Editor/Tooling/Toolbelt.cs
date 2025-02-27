using UnityEngine;
using UnityEngine.UI;

public class Toolbelt : MonoBehaviour
{
    public PanoramaSphereController PanoramaSphereController;
    public CameraHandler cameraHandler;

    private Tool currentTool;
    public Tool CurrentTool { get { return currentTool; } set { currentTool = value; } }

    private void Start()
    {
        foreach(Toggle t in GetComponentsInChildren<Toggle>())
        {
            if (!t.isOn) continue;
            currentTool = t.gameObject.GetComponent<Tool>();
            break;
        }
    }
}
