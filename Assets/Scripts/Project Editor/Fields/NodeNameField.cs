using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeNameField", menuName = "ConfigFields/NodeNameField")]
public class NodeNameField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.currentNode.uniqueName;
    }
    public override string SetField(ProjectContext context, string value)
    {
        if (context.Config.nodes.Select(node => node.uniqueName).Any(name => name.Equals(value)))
            throw new Exception($"Node name {value} is already in use");

        context.currentNode.uniqueName = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public override bool IsInputReady(ProjectContext context)
    {
        return context.currentNode != null;
    }
}
