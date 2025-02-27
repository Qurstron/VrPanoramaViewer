using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnglePoint
{
    /// <summary>
    /// The Angle in the UI
    /// </summary>
    public abstract Vector2 Angle { get; set; }
    /// <summary>
    /// The Angle in the json file
    /// </summary>
    public abstract Vector2 JsonAngle { get; set; }
    public WorldSelectable relatedComponent;
}

public class DummyPoint : AnglePoint
{
    public override Vector2 Angle { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public override Vector2 JsonAngle { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
}
