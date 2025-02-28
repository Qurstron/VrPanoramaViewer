using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AddOnLabelField", menuName = "ConfigFields/AddOnLabelField")]
public class AddOnLabelField : ConfigField<string>, IDynamicField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.selectedObjects[0].LabelJson;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.selectedObjects[0].LabelJson = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public void SetFieldDynamic(ProjectContext context, string value)
    {
        context.selectedObjects[0].Label = value;
    }

    public override bool IsInputReady(ProjectContext context)
    {
        return context.selectedObjects.Count == 1;
    }
}
