using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OutlineWidthField", menuName = "ConfigFields/AddOns/OutlineWidthField")]
public class OutlineWidthField : ConfigField<float>, IDynamicField<float>
{
    public override float GetField(ProjectContext context)
    {
        return context.selectedObjects[0].OutlineWidthJson;
    }
    public override float SetField(ProjectContext context, float value)
    {
        context.selectedObjects[0].OutlineWidthJson = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public void SetFieldDynamic(ProjectContext context, float value)
    {
        context.selectedObjects[0].OutlineWidth = value;
    }

    public override bool IsInputReady(ProjectContext context)
    {
        return context.selectedObjects.Count == 1;
    }
}
