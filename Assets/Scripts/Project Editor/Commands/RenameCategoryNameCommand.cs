using UnityEngine;

public class RenameCategoryNameCommand : IDirtyCommand
{
    private readonly int index;
    public string oldName, newName;

    public RenameCategoryNameCommand(int index, string newName)
    {
        this.index = index;
        this.newName = newName;
    }

    public bool Execute(ProjectContext context)
    {
        oldName = context.Config.categoryNames[index];
        context.Config.categoryNames[index] = newName;
        return true;
    }

    public void Undo(ProjectContext context)
    {
        context.Config.categoryNames[index] = oldName;
    }
}
