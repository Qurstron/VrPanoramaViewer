using UnityEngine;

[CreateAssetMenu(fileName = "OutlineColorField", menuName = "ConfigFields/AddOns/OutlineColorField")]
public class OutlineColorField : ConfigField<string>, IDynamicField<string>
{
    public override string GetField(ProjectContext context)
    {
        return QUtils.FormatHexColor(context.selectedObjects[0].OutlineColorJson);
    }
    public override string SetField(ProjectContext context, string value)
    {
        value = QUtils.FormatHexColor(value);
        context.selectedObjects[0].OutlineColorJson = value;
        onFieldChange.Invoke(value);
        return value;
    }

    public void SetFieldDynamic(ProjectContext context, string value)
    {
        context.selectedObjects[0].OutlineColor = value;
    }

    public override bool IsInputReady(ProjectContext context)
    {
        return context.selectedObjects.Count == 1;
    }
}
