using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LabelHeaderField", menuName = "ConfigFields/LabelHeaderField")]
public class LabelHeaderField : ConfigField<string>, IDynamicField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.currentLabel.JsonHeader;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.currentLabel.JsonHeader = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public void SetFieldDynamic(ProjectContext context, string value)
    {
        context.currentLabel.Header = value;
    }
}
