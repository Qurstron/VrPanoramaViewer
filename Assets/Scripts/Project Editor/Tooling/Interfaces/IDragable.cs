using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDragableAngle
{
    public abstract void Drag(Vector2 angle, Vector2 deltaAngle);
}
