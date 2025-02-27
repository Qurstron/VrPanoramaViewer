using UnityEngine;

/// <summary>
/// A abstract create and delete command.
/// Usually these actions are mostly inverses of each other and can
/// be merged into one command.
/// </summary>
public abstract class CDCommand : IDirtyCommand
{
    bool isDeleting = false;

    public CDCommand(bool isDeleting)
    {
        this.isDeleting = isDeleting;
    }


    public bool Execute(ProjectContext context)
    {
        if (isDeleting) return DExecute(context);
        return CExecute(context);
    }
    public void Undo(ProjectContext context)
    {
        if (isDeleting) DUndo(context);
        else CUndo(context);
    }

    public abstract bool CExecute(ProjectContext context);
    public abstract void CUndo(ProjectContext context);
    public abstract bool DExecute(ProjectContext context);
    public abstract void DUndo(ProjectContext context);
}
