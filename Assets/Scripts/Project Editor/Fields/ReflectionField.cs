using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ReflectionField", menuName = "ConfigFields/ReflectionField")]
public class ReflectionField<T> : ConfigField<T>
{
    [SerializeField] private string contextReflectionPath;
    //private string[] reflections;

    //private void OnEnable()
    //{
    //    reflections = reflectionPath.Split('.');
    //}
    //private void OnValidate()
    //{
    //    OnEnable();
    //}

    public override T GetField(ProjectContext context)
    {
        throw new NotImplementedException();
        //return (T)typeof(ProjectContext).GetField(_name).GetValue(this, null);
    }
    public override T SetField(ProjectContext context, T value)
    {
        throw new NotImplementedException();
        //if (!IsInputReady(context)) return Vector2.negativeInfinity;

        //Vector2 dif = value - GetField(context);
        //foreach (var angle in context.selectedAngles)
        //{
        //    angle.JsonAngle += dif;
        //}

        //onFieldChange.Invoke(value);
        //return value;
    }
}
