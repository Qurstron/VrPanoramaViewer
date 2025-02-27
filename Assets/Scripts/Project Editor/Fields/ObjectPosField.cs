using JSONClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectPosField", menuName = "ConfigFields/ObjectPosField")]
public class ObjectPosField : ConfigField<Vector3>, IDynamicField<Vector3>
{
    public override Vector3 GetField(ProjectContext context)
    {
        //JSONTransform jTransform = new();
        Vector3 avgPosition = Vector3.zero;

        foreach (ObjectSelectable objectSelectable in context.selectedObjects)
        {
            avgPosition += objectSelectable.JsonPosition;
        }
        avgPosition /= context.selectedObjects.Count;
        //jTransform.translation = new float[] { avgPosition.x, avgPosition.y, avgPosition.z };

        return avgPosition;
    }
    public override Vector3 SetField(ProjectContext context, Vector3 value)
    {
        if (!IsInputReady(context)) return Vector3.negativeInfinity;

        Vector3 dif = value - GetField(context);
        foreach (var obj in context.selectedObjects)
        {
            obj.JsonPosition += dif;
        }

        onFieldChange.Invoke(value);
        return value;
    }
    public override bool IsInputReady(ProjectContext context)
    {
        return context.selectedObjects.Count > 0;
    }

    public void SetFieldDynamic(ProjectContext context, Vector3 value)
    {
        if (!IsInputReady(context)) return;

        Vector3 dif = value - GetField(context);
        foreach (var obj in context.selectedObjects)
        {
            obj.Position = dif;
        }
    }
}
