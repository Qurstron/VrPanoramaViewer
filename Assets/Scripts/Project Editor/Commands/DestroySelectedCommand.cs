using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroySelectedCommand : IDirtyCommand
{
    private List<AnglePoint> points; 

    public DestroySelectedCommand()
    {

    }

    public bool Execute(ProjectContext context)
    {
        points = new(context.selectedAngles);

        //foreach (AnglePoint point in context.selectedAngles)
        //{
        //    context.editor.des
        //}
        return true;
    }

    public void Undo(ProjectContext context)
    {
        //foreach (AnglePoint point in context.selectedAngles)
        //{
        //    point.Angle -= delta;
        //}
    }
}
