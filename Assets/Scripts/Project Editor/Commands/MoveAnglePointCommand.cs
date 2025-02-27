using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveAnglePointCommand : IDirtyCommand
{
    private readonly Vector2 delta;
    //private bool wasExecuted = false;

    public MoveAnglePointCommand(Vector2 delta)
    {
        this.delta = delta;
    }

    public bool Execute(ProjectContext context)
    {
        if (delta.SqrMagnitude() == 0) return false;
        //if (!wasExecuted)
        //{
        //    wasExecuted = true;
        //    return true;
        //}

        foreach (AnglePoint point in context.selectedAngles)
        {
            point.JsonAngle += delta;
        }
        return true;
    }

    public void Undo(ProjectContext context)
    {
        foreach (AnglePoint point in context.selectedAngles)
        {
            point.JsonAngle -= delta;
        }
    }
}
