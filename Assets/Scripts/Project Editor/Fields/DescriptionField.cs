using UnityEngine;

[CreateAssetMenu(fileName = "DescriptionField", menuName = "ConfigFields/DescriptionField")]
public class DescriptionField : ConfigField<string>
{
    public override string GetField(ProjectContext context)
    {
        return context.Config.description;
    }
    public override string SetField(ProjectContext context, string value)
    {
        context.Config.description = value;
        onFieldChange.Invoke(value);
        return value;
    }
}
