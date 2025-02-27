using JSONClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Similar in nature to the mediator programmingpattern
/// </summary>
public class ProjectContext
{
    public UnityEvent OnSelectionChange = new();

    public UnityEvent OnConfigChange = new();
    public UnityEvent OnNodeChange = new();
    public UnityEvent OnNodeContentChange = new();
    public UnityEvent OnObjectChange = new();
    public UnityEvent OnCategoryNameChange = new();
    public UnityEvent OnGraphUpdate = new();

    public Config Config { get; private set; }
    public Node currentNode;
    public NodeContent currentNodeContent;
    public LineComponent currentLine;
    public LabelComponent currentLabel;

    public readonly List<AnglePoint> selectedAngles = new();
    public readonly List<ObjectSelectable> selectedObjects = new();
    public readonly ProjectEditor editor;

    //private NodeContent fullNodeConte

    /// <summary>
    /// Gets the average angle of the SelectedAngles. Is Vector2.negativeInfinity if no Angles are selected
    /// </summary>
    public Vector2 AvgSelectedAngles {
        get
        {
            if (selectedAngles.Count == 0) return Vector2.negativeInfinity;
            return selectedAngles.Select(ap => ap.Angle).Aggregate((a, b) => a + b) / selectedAngles.Count;
        }
    }
    /// <summary>
    /// Gets the average world position of the SelectedObjects. Is Vector3.negativeInfinity if no Angles are selected
    /// </summary>
    public Vector3 AvgSelectedObjectPos
    {
        get
        {
            if (selectedObjects.Count == 0) return Vector3.negativeInfinity;
            return selectedObjects.Select(obj => obj.transform.position).Aggregate((a, b) => a + b) / selectedObjects.Count;
        }
    }
    public NodeContent FullNodeContent { get; private set; }

    public ProjectContext(ProjectEditor editor, Config config)
    {
        this.editor = editor;
        Config = config;

        OnConfigChange.AddListener(OnNodeChange.Invoke);
        OnNodeChange.AddListener(OnNodeContentChange.Invoke);
        OnNodeContentChange.AddListener(OnObjectChange.Invoke);
        OnNodeContentChange.AddListener(() =>
        {
            editor.ExecuteCommand(SelectCommand.DeselectAll());

            FullNodeContent = currentNodeContent.GetFullObj();
        });
    }
}
