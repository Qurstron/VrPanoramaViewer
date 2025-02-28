using System.Collections.Generic;
using UnityEngine;

public class Selection : Tool, IClickableAngle, IDownableAngle, IUpableAngle, IDragableAngle
{
    [SerializeField] private float maxSelectionDistance = 1.0f;
    private Vector2 downAngle = Vector2.zero;
    private bool wasDraged = false;

    public void Click(Vector2 angle)
    {
        if (downAngle != angle) return;
        bool isClearing = !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift));
        float closestDistance = maxSelectionDistance * toolbelt.cameraHandler.FOV / 90;
        AnglePoint closestAnglePoint = null;

        foreach (AngleSelectable selectable in SphereController.GetApperenceObject())
        {
            foreach (var point in selectable.GetPoints())
            {
                float distance = Vector2.Distance(angle, point.Angle);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestAnglePoint = point;
                }
            }
        }

        if (closestAnglePoint != null)
        {
            Context.editor.ExecuteCommand(new SelectCommand(new List<AnglePoint>() { closestAnglePoint }, isClearing));
        }
        else if (isClearing)
        {
            Context.editor.ExecuteCommand(SelectCommand.DeselectAll());
            //Context.editor.ExecuteCommand(new SelectCommand(new List<AnglePoint>(), isClearing));
        }
    }

    public void Down(Vector2 angle)
    {
        downAngle = angle;
    }

    public void Up(Vector2 angle)
    {
        if (downAngle == angle) return;
        List<AnglePoint> points = new();
        Vector2 min = Vector2.Min(downAngle, angle);
        Vector2 max = Vector2.Max(downAngle, angle);
        bool isClearing = wasDraged && !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift));
        wasDraged = false;

        foreach (AngleSelectable selectable in SphereController.GetApperenceObject())
        {
            foreach (var point in selectable.GetPoints())
            {
                Vector2 objAngle = point.Angle;
                if (min.x <= objAngle.x && min.y <= objAngle.y && max.x >= objAngle.x && max.y >= objAngle.y)
                {
                    points.Add(point);
                }
            }
        }

        Context.editor.ExecuteCommand(new SelectCommand(points, isClearing));
        SphereController.setAreaSelectLine(null);
    }

    public void Drag(Vector2 angle, Vector2 deltaAngle)
    {
        wasDraged = true;

        List<Vector2> corners = new()
            {
                downAngle,
                new(downAngle.x, angle.y),
                angle,
                new(angle.x, downAngle.y)
            };

        SphereController.setAreaSelectLine(corners);
    }
}
