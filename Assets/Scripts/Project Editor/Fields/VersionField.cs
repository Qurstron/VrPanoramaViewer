using UnityEngine;

[CreateAssetMenu(fileName = "VersionField", menuName = "ConfigFields/VersionField")]
public class VersionField : ConfigField<long>
{
    public override long GetField(ProjectContext context)
    {
        return context.Config.version;
    }
    public override long SetField(ProjectContext context, long value)
    {
        if (value < 0) value = 0;

        context.Config.version = value;
        onFieldChange.Invoke(value);
        return value;
    }
}
