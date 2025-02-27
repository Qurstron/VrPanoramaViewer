using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scale : Tool, IDownableAngle, IDragableAngle
{
    private Vector2 avgAngle = Vector2.zero;

    public void Down(Vector2 angle)
    {
        //avgAngle = Vector2.zero;
        //foreach (AnglePoint point in toolbelt.selectedObjs)
        //{
        //    avgAngle = point.Angle;
        //}
        //avgAngle /= toolbelt.selectedObjs.Count;
    }

    public void Drag(Vector2 angle, Vector2 deltaAngle)
    {
        //if (toolbelt.selectedObjs.Count <= 1) return;

        //foreach (AnglePoint point in toolbelt.selectedObjs)
        //{

        //    point.Angle += deltaAngle;
        //}
        //toolbelt.UpdateHighlight();
    }
}
