using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LineColorField", menuName = "ConfigFields/LineColorField")]
public class LineColorField : ConfigField<string>, IDynamicField<string>
{
    public override string GetField(ProjectContext context)
    {
        return QUtils.FormatHexColor(context.currentLine.JsonColor);
    }
    public override string SetField(ProjectContext context, string value)
    {
        value = QUtils.FormatHexColor(value);
        context.currentLine.JsonColor = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public void SetFieldDynamic(ProjectContext context, string value)
    {
        context.currentLine.Color = value;
    }
}
