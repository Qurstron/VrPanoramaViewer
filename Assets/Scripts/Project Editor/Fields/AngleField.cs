using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AngleField", menuName = "ConfigFields/AngleField")]
public class AngleField : ConfigField<Vector2>, IDynamicField<Vector2>
{
    public override Vector2 GetField(ProjectContext context)
    {
        return context.AvgSelectedAngles;
    }
    public override Vector2 SetField(ProjectContext context, Vector2 value)
    {
        if (!IsInputReady(context)) return Vector2.negativeInfinity;

        Vector2 dif = value - GetField(context);
        foreach (var angle in context.selectedAngles)
        {
            angle.JsonAngle += dif;
        }

        onFieldChange.Invoke(value);
        return value;
    }
    public override bool IsInputReady(ProjectContext context)
    {
        return context.selectedAngles.Count > 0;
    }

    public void SetFieldDynamic(ProjectContext context, Vector2 value)
    {
        if (context.selectedAngles.Count <= 0) return;

        Vector2 dif = value - GetField(context);
        foreach (var angle in context.selectedAngles)
        {
            angle.Angle += dif;
        }
    }
}
