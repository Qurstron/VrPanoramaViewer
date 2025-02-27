using UnityEngine;

public class Move : Tool, IDragableAngle, IDownableAngle, IUpableAngle
{
    private Vector2 angleOffset = Vector2.zero;

    public void Down(Vector2 angle)
    {
        angleOffset = Vector2.zero;
    }

    public void Drag(Vector2 angle, Vector2 deltaAngle)
    {
        foreach (AnglePoint point in Context.selectedAngles)
        {
            angleOffset += deltaAngle;
            point.Angle += deltaAngle;
        }
    }

    public void Up(Vector2 angle)
    {
        Context.editor.ExecuteCommand(new MoveAnglePointCommand(angleOffset));
    }

    //public void Up(Vector2 angle)
    //{
    //    // TODO: update selected obj in timeline to save changes
    //    foreach (GameObject obj in toolbelt.selectedObjs)
    //    {
    //        //foreach (var point in obj.GetComponent<ISelectable>().GetPoints())
    //        //{
    //        //    Vector2 pos = point.Angle;
    //        //    labelComp.Label.pos = new float[] { pos.x, pos.y };
    //        //}
    //        //var labelComp = obj.GetComponent<LabelComponent>();
    //        //if (labelComp == null) continue;

    //        //Vector2 pos = obj.GetComponent<Angler>().Angle;
    //        //labelComp.Label.pos = new float[] { pos.x, pos.y };
    //    }
    //}
}
