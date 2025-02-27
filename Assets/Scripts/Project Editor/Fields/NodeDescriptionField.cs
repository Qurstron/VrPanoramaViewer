using UnityEngine;

[CreateAssetMenu(fileName = "NodeDescriptionField", menuName = "ConfigFields/NodeDescriptionField")]
public class NodeDescriptionField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.currentNode.description;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.currentNode.description = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public override bool IsInputReady(ProjectContext context)
    {
        return context.currentNode != null;
    }
}
