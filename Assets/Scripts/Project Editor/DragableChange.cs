using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class DragableChange : Selectable, IDragHandler
{
    private Vector2 pointerPos = Vector2.zero;
    public bool WasDraged { get; private set; } = false;

    public virtual void OnDrag(PointerEventData eventData)
    {
        WasDraged = true;
        OnDeltaDrag(eventData.position - pointerPos, eventData);
        pointerPos = eventData.position;
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        pointerPos = eventData.position;
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (WasDraged)
        {
            WasDraged = false;
            OnRelease();
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="deltaPos">The relative change to last frame</param>
    public abstract void OnDeltaDrag(Vector2 deltaPos, PointerEventData eventData);
    /// <summary>
    /// Gets called on pointer up when the component was draged
    /// </summary>
    public virtual void OnRelease() { }
}
