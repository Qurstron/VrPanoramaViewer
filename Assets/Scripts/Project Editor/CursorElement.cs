using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CursorElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private CursorHandler.CursorType type = CursorHandler.CursorType.Default;
    private CursorHandler cursorHandler;

    private void Awake()
    {
        cursorHandler = transform.root.GetComponent<CursorHandler>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        cursorHandler.SetCursor(type);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        cursorHandler.Unset();
    }

    private void OnDestroy()
    {
        if (cursorHandler != null) cursorHandler.Unset();
    }
    private void OnDisable()
    {
        if (cursorHandler != null) cursorHandler.Unset();
    }
}
