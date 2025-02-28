using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public abstract class Tool : ConfigActor
{
    public bool IsActive
    {
        get
        {
            return toolbelt.CurrentTool == this;
        }
    }
    protected PanoramaSphereController SphereController { get { return toolbelt.PanoramaSphereController; } }
    //protected TimelineController TimelineController { get { return toolbelt.timelineController; } }
    protected Toolbelt toolbelt;
    private Toggle toggle;

    private void Awake()
    {
        toolbelt = GetComponentInParent<Toolbelt>();

        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener((value) =>
        {
            if (!value || toolbelt.CurrentTool == this) return;

            toolbelt.CurrentTool?.OnExit();
            toolbelt.CurrentTool = this;
            OnEnter();
        });
    }

    public void Select()
    {
        toggle.isOn = true;
    }

    public virtual void OnExit()
    {

    }
    public virtual void OnEnter()
    {

    }
}
