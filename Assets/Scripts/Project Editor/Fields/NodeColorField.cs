using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeColorField", menuName = "ConfigFields/NodeColorField")]
public class NodeColorField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return QUtils.FormatHexColor(context.currentNode.color);
    }
    public override string SetField(ProjectContext context, string value)
    {
        value = QUtils.FormatHexColor(value);
        context.currentNode.color = value;
        //context.currentNode.Color = QUtils.StringToColor(value);
        onFieldChange.Invoke(value);
        return value;
    }

    public override bool IsInputReady(ProjectContext context)
    {
        return context.currentNode != null;
    }
}
