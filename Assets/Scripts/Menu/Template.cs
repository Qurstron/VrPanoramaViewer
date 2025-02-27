using UnityEngine;

[CreateAssetMenu(fileName = "Template", menuName = "Project Template")]
public class Template : ScriptableObject
{
    public string assetPath;
    public string projectName;
    public string description;
    public Sprite preview;
}
