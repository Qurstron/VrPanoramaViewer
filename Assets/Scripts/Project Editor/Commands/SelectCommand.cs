using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectCommand : ICommand
{
    private List<AnglePoint> angles;
    private List<AnglePoint> oldAngles;
    private List<ObjectSelectable> objects;
    private List<ObjectSelectable> oldObjects;
    private bool isClearing;
    private bool isFullClear = false;
    private readonly bool isFocusOnAngles;

    public static SelectCommand DeselectAll()
    {
        return new SelectCommand()
        {
            angles = new(),
            objects = new(),
            isClearing = true,
            isFullClear = true,
        };
    }
    public SelectCommand(List<AnglePoint> angles, bool isClearing = false)
    {
        this.angles = angles;
        this.isClearing = isClearing;
        isFocusOnAngles = true;
    }
    public SelectCommand(List<ObjectSelectable> objects, bool isClearing = false)
    {
        this.objects = objects;
        this.isClearing = isClearing;
        isFocusOnAngles = false;
    }
    private SelectCommand()
    {

    }

    public bool Execute(ProjectContext context)
    {
        bool result;

        if (isFullClear) result = Deselect(context);
        else if (isFocusOnAngles) result = SelectAngles(context);
        else result = SelectObjects(context);

        if (!result) return false;

        CheckAndSendUnique(context);
        context.OnSelectionChange.Invoke();

        return true;
    }
    public void Undo(ProjectContext context)
    {
        context.selectedAngles.Clear();
        context.selectedAngles.AddRange(oldAngles);
        CheckAndSendUnique(context);

        SetSelected(context, false);
        context.selectedObjects.Clear();
        context.selectedObjects.AddRange(oldObjects);
        SetSelected(context, true);

        context.OnSelectionChange.Invoke();
    }

    private bool SelectAngles(ProjectContext context)
    {
        // nothing can change, so ignore this command
        if (angles != null)
        {
            if (Enumerable.SequenceEqual(context.selectedAngles, angles)) return false;
        }
        else if (!isClearing || (isClearing && context.selectedAngles.Count <= 0))
        {
            return false;
        }

        oldAngles = new(context.selectedAngles);

        if (isClearing) context.selectedAngles.Clear();
        if (angles != null) context.selectedAngles.AddRange(angles);

        SetSelected(context, false);
        oldObjects = new(context.selectedObjects);
        context.selectedObjects.Clear();

        return true;
    }
    private bool SelectObjects(ProjectContext context)
    {
        // nothing can change, so ignore this command
        if (Enumerable.SequenceEqual(context.selectedObjects, objects)) return false;

        oldObjects = new(context.selectedObjects);

        if (isClearing)
        {
            SetSelected(context, false);
            context.selectedObjects.Clear();
        }
        context.selectedObjects.AddRange(objects);
        SetSelected(context, true);

        oldAngles = new(context.selectedAngles);
        context.selectedAngles.Clear();

        return true;
    }
    private bool Deselect(ProjectContext context)
    {
        if (context.selectedAngles.Count <= 0 && context.selectedObjects.Count <= 0)
            return false;

        SelectAngles(context);
        SelectObjects(context);

        return true;
    }

    private void CheckAndSendUnique(ProjectContext context)
    {
        context.editor.SetSelectionDirty();
        if (context.selectedAngles.Count <= 0)
        {
            context.editor.SetUniqueCard(null);
            return;
        }

        AngleSelectable selectable = context.selectedAngles[0].relatedComponent;
        foreach (var point in context.selectedAngles)
        {
            if (selectable != point.relatedComponent)
            {
                context.editor.SetUniqueCard(null);
                return;
            }
        }

        context.editor.SetUniqueCard(selectable);
    }
    private void SetSelected(ProjectContext context, bool isSelected)
    {
        foreach (ObjectSelectable obj in context.selectedObjects)
        {
            obj.IsSelected = isSelected;
        }
    }
}
