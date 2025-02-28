using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TitleField", menuName = "ConfigFields/TitleField")]
public class TitleField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.Config.name;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.Config.name = value;
        onFieldChange.Invoke(value);
        return value;
    }
}
