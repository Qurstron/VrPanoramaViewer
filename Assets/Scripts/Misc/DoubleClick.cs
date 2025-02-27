using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DoubleClick : MonoBehaviour, IPointerClickHandler, IPointerExitHandler
{
    // https://en.wikipedia.org/wiki/Double-click
    // https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setdoubleclicktime?redirectedfrom=MSDN
    [Tooltip("Microsoft uses a default of 500ms")]
    [SerializeField] private int maxMsBetweenClicks = 500;
    public UnityEvent<Vector2> OnDoubleClick;
    private Stopwatch stopWatch;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (stopWatch == null)
        {
            stopWatch = new();
            stopWatch.Start();
            return;
        }

        stopWatch.Stop();
        if (stopWatch.Elapsed.TotalMilliseconds < maxMsBetweenClicks)
        {
            OnDoubleClick.Invoke(Input.mousePosition);
        }
        stopWatch = null;
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        stopWatch = null;
    }
}
