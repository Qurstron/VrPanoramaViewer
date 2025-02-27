using UnityEngine;

[CreateAssetMenu(fileName = "ObjectScaleField", menuName = "ConfigFields/ObjectScaleField")]
public class ObjectScaleField : ConfigField<Vector3>, IDynamicField<Vector3>
{
    public override Vector3 GetField(ProjectContext context)
    {
        if (context.selectedObjects.Count <= 0)
            return Vector3.zero;
        return context.selectedObjects[0].JsonScale;
    }
    public override Vector3 SetField(ProjectContext context, Vector3 value)
    {
        if (!IsInputReady(context)) return Vector3.negativeInfinity;

        foreach (var obj in context.selectedObjects)
        {
            obj.JsonScale = value;
        }

        onFieldChange.Invoke(value);
        return value;
    }
    public override bool IsInputReady(ProjectContext context)
    {
        return context.selectedObjects.Count > 0;
    }

    public void SetFieldDynamic(ProjectContext context, Vector3 value)
    {
        if (!IsInputReady(context)) return;

        foreach (var obj in context.selectedObjects)
        {
            obj.Scale = value;
        }
    }
}
