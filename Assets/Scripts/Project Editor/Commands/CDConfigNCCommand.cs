using JSONClasses;

public class CDConfigNCCommand : CDCommand
{
    private NodeContent nc;

    public CDConfigNCCommand(NodeContent nc, bool isDeleting) : base(isDeleting)
    {
        this.nc = nc;
    }

    public override bool CExecute(ProjectContext context)
    {
        context.Config.categoryParents.Add(nc);
        context.OnNodeContentChange.Invoke();

        return true;
    }
    public override void CUndo(ProjectContext context)
    {
        context.Config.categoryParents.RemoveAt(context.Config.categoryParents.Count - 1);
        context.OnNodeContentChange.Invoke();
    }

    public override bool DExecute(ProjectContext context)
    {
        CUndo(context);
        return true;
    }
    public override void DUndo(ProjectContext context)
    {
        CExecute(context);
    }
}
