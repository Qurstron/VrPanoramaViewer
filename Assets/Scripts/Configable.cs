using JSONClasses;
using UnityEngine;

public abstract class Configable : MonoBehaviour
{
    public abstract void SetConfig(Config config, bool tryKeepIndices = false);
    public Config Config { protected get; set; }
}
