using UnityEngine;

[CreateAssetMenu(fileName = "LineWidthField", menuName = "ConfigFields/LineWidthField")]
public class LineWidthField : ConfigField<float>, IDynamicField<float>
{
    public override float GetField(ProjectContext context)
    {
        return context.currentLine.JsonWidth;
    }
    public override float SetField(ProjectContext context, float value)
    {
        context.currentLine.JsonWidth = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public void SetFieldDynamic(ProjectContext context, float value)
    {
        context.currentLine.Width = value;
    }
}
