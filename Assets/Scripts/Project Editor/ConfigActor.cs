using UnityEngine;
using UnityEngine.Events;

public abstract class ConfigActor : MonoBehaviour
{
    private ProjectContext context;
    public ProjectContext Context
    {
        protected get { return context; }
        set
        {
            context = value;

            OnContextChange();
        }
    }

    /// <returns>The coresponding event based on the FieldUpdateLevel</returns>
    protected UnityEvent GetUpdateEvent(FieldUpdateLevel level)
    {
        return level switch
        {
            FieldUpdateLevel.Node => Context.OnNodeChange,
            FieldUpdateLevel.NodeContent => Context.OnNodeContentChange,
            FieldUpdateLevel.Object => Context.OnObjectChange,
            FieldUpdateLevel.Selection => Context.OnSelectionChange,
            _ => Context.OnConfigChange,
        };
    }
    /// <summary>
    /// Gets called when the Context changes a.k.a. when a new config was loaded
    /// </summary>
    protected virtual void OnContextChange() { }
}
