using JSONClasses;
using System;
using UnityEngine;

public class CDNodeContentIndexCommand : CDCommand
{
    private int index;
    private string name;

    /// <summary>
    /// Delete NodeContentIndex
    /// </summary>
    public CDNodeContentIndexCommand(int index) : base(true)
    {
        this.index = index;
    }
    /// <summary>
    /// Create NodeContentIndex
    /// </summary>
    public CDNodeContentIndexCommand(string name) : base(false)
    {
        this.name = name;
    }

    public override bool CExecute(ProjectContext context)
    {
        context.Config.categoryNames.Add(name);
        context.OnCategoryNameChange.Invoke();
        return true;
    }
    public override void CUndo(ProjectContext context)
    {
        context.Config.categoryNames.RemoveAt(context.Config.categoryNames.Count - 1);
        context.OnCategoryNameChange.Invoke();
    }

    public override bool DExecute(ProjectContext context)
    {
        if (index >= context.Config.categoryNames.Count) return false;

        name = context.Config.categoryNames[index];
        context.Config.categoryNames.RemoveAt(index);

        context.OnCategoryNameChange.Invoke();
        return true;
    }
    public override void DUndo(ProjectContext context)
    {
        context.Config.categoryNames.Insert(index, name);
        context.OnCategoryNameChange.Invoke();
    }
}
