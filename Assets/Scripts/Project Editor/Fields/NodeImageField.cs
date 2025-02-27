using UnityEngine;

[CreateAssetMenu(fileName = "NodeImageField", menuName = "ConfigFields/NodeImageField")]
public class NodeImageField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.currentNodeContent.texture;
    }
    public override string SetField(ProjectContext context, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) value = null;

        context.currentNodeContent.texture = value;
        context.OnNodeContentChange.Invoke();
        onFieldChange.Invoke(value);
        return value;
    }
}
