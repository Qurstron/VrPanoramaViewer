using JSONClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

[CreateAssetMenu(fileName = "ObjectRotField", menuName = "ConfigFields/ObjectRotField")]
public class ObjectRotField : ConfigField<Vector3>, IDynamicField<Vector3>
{
    public override Vector3 GetField(ProjectContext context)
    {
        if (context.selectedObjects.Count <= 0) return Vector3.zero;
        return context.selectedObjects[0].JsonRotation.eulerAngles;
    }
    public override Vector3 SetField(ProjectContext context, Vector3 value)
    {
        if (!IsInputReady(context)) return Vector3.negativeInfinity;

        foreach (var obj in context.selectedObjects)
        {
            obj.JsonRotation = Quaternion.Euler(value);
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

        foreach (var obj in context.selectedObjects)
        {
            obj.Rotation = Quaternion.Euler(value);
        }
    }
}
