using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeDisplayNameField", menuName = "ConfigFields/NodeDisplayNameField")]
public class NodeDisplayNameField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.currentNode.name;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.currentNode.name = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public override bool IsInputReady(ProjectContext context)
    {
        return context.currentNode != null;
    }
}
