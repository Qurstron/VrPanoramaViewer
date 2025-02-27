using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JSONClasses;

public interface IConfigable
{
    public abstract void SetConfig(Config config, bool tryKeepIndices = false);
    public Config Config { get; }
}
