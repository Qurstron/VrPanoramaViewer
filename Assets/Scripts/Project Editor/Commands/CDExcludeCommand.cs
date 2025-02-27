using JSONClasses;
using System;
using UnityEngine;

public class CDExcludeCommand : CDCommand
{
    private MergeSubject mergeSubject;

    public CDExcludeCommand(MergeSubject mergeSubject, bool isDeleting) : base(isDeleting)
    {
        this.mergeSubject = mergeSubject;
    }

    public override bool CExecute(ProjectContext context)
    {
        if (string.IsNullOrEmpty(mergeSubject.unquieID))
            mergeSubject.unquieID = Guid.NewGuid().ToString();
        if (context.currentNodeContent.excludes.Contains(mergeSubject.unquieID))
            return false;

        context.currentNodeContent.excludes.Add(mergeSubject.unquieID);
        context.OnNodeContentChange.Invoke();

        return true;
    }
    public override void CUndo(ProjectContext context)
    {
        context.currentNodeContent.excludes.Remove(mergeSubject.unquieID);
        context.OnNodeContentChange.Invoke();
    }

    public override bool DExecute(ProjectContext context)
    {
        if (!context.currentNodeContent.excludes.Contains(mergeSubject.unquieID))
            return false;

        CUndo(context);

        return true;
    }
    public override void DUndo(ProjectContext context)
    {
        CExecute(context);
    }
}
