using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSONClasses;

public abstract class Configable : MonoBehaviour
{
    public abstract void SetConfig(Config config, bool tryKeepIndices = false);
    public Config Config { protected get; set; }
}
