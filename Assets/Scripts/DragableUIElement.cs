using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using static UnityEngine.EventSystems.PointerEventData;

public class DragableUIElement : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private InputButton button = InputButton.Left;
    [SerializeField] private bool consumeEvent = true;
    [Tooltip("Drage the object if any mouse button except the specified button is pressed")]
    [SerializeField] private bool blacklistButton = false;
    [Tooltip("Creates a copy that gets draged")]
    [SerializeField] private bool dragCopy = false;
    [Tooltip("ReParents the DragableUIElement to Canvas root during the drag")]
    [SerializeField] private bool reParent = false;
    public Transform parent;

    public UnityEvent<Vector2, GameObject> onBeginDrag;
    public UnityEvent<Vector2, GameObject> onDrag;
    public UnityEvent<Vector2, GameObject> onFinishedDrag;
    public bool IsDraged { private set; get; } = false;

    private bool isDragedCopy = false;
    private DragableUIElement dragableCopy = null;

    public void OnDrag(PointerEventData eventData)
    {
        Drag(eventData);
    }
    public void Drag(BaseEventData baseEvent)
    {
        if (((baseEvent as PointerEventData).button != button) ^ blacklistButton) return;
        //if (dragCopy && !isDragedCopy) return;
        Transform target = dragCopy ? dragableCopy.transform : transform;

        target.position = Input.mousePosition * (Vector2)target.parent.localScale;
        onDrag.Invoke(target.localPosition, gameObject);
        if (consumeEvent) baseEvent.Use();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        BeginDrag(eventData);
    }
    public void BeginDrag(BaseEventData baseEvent)
    {
        IsDraged = true;
        onBeginDrag.Invoke(transform.localPosition, gameObject);

        if (dragCopy && !isDragedCopy)
        {
            Transform actualParent = transform.parent;
            if (reParent)
            {
                if (parent == null)
                    actualParent = GetComponentInParent<Canvas>().rootCanvas.transform;
                else
                    actualParent = parent;
            }

            GameObject copy = Instantiate(gameObject, actualParent);
            dragableCopy = copy.GetComponent<DragableUIElement>();
            dragableCopy.isDragedCopy = true;
            (dragableCopy.transform as RectTransform).sizeDelta = (transform as RectTransform).sizeDelta;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDrag(eventData);
    }
    public void EndDrag(BaseEventData baseEvent)
    {
        IsDraged = false;
        onFinishedDrag.Invoke(transform.localPosition, gameObject);

        if (dragableCopy != null) Destroy(dragableCopy.gameObject);
        dragableCopy = null;
    }
}
