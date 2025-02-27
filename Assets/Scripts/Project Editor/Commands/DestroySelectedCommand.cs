using System.Collections.Generic;

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
