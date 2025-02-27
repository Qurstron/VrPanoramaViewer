using JSONClasses;
using System;
using System.Collections.Generic;
using UnityEngine;

public class DeleteWorldObjectCommand : CDCommand, IDirtyCommand
{
    private List<AnglePoint> anglePoints;

    public DeleteWorldObjectCommand(List<AnglePoint> worldObjs, bool isDeleting = false) : base(isDeleting)
    {
        anglePoints = new(worldObjs);
    }

    public override bool CExecute(ProjectContext context)
    {
        HashSet<WorldSelectable> closedList = new();

        foreach (var anglePoint in anglePoints)
        {
            if (closedList.Contains(anglePoint.relatedComponent)) continue;
            anglePoint.relatedComponent.Add();
            closedList.Add(anglePoint.relatedComponent);
        }
        context.OnNodeContentChange.Invoke();

        return true;
    }
    public override void CUndo(ProjectContext context)
    {
        HashSet<WorldSelectable> closedList = new();

        foreach (var anglePoint in anglePoints)
        {
            if (closedList.Contains(anglePoint.relatedComponent)) continue;
            anglePoint.relatedComponent.Remove();
            closedList.Add(anglePoint.relatedComponent);
        }
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

    // https://stackoverflow.com/questions/457676/check-if-a-class-is-derived-from-a-generic-class
    private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
    {
        while (toCheck != null && toCheck != typeof(object))
        {
            var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
            if (generic == cur)
            {
                return true;
            }
            toCheck = toCheck.BaseType;
        }
        return false;
    }
}
