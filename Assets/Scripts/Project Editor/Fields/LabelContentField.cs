using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LabelContentField", menuName = "ConfigFields/LabelContentField")]
public class LabelContentField : ConfigField<string>, IDynamicField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.currentLabel.JsonContent;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.currentLabel.JsonContent = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public void SetFieldDynamic(ProjectContext context, string value)
    {
        context.currentLabel.Content = value;
    }
}
