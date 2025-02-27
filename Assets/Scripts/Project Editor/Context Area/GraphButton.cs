using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JSONClasses;

[RequireComponent(typeof(DragableUIElement))]
public class GraphButton : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
{
    [SerializeField] private Graphic grahpicColorDisplay;
    private DragableUIElement dragable;
    private GraphNavigator graphNavigator;
    private Node node;
    private bool isDragging;

    public Vector3 Force = Vector3.zero;

    public bool IsHeld
    {
        get
        {
            if (dragable == null) return false;
            return dragable.IsDraged;
        }
    }
    public Node Node
    {
        get { return node; }
        set
        {
            node = value;
            //grahpicColorDisplay.color = node.Color;
            UpdateColor();
        }
    }

    private void Start()
    {
        dragable = GetComponent<DragableUIElement>();
        graphNavigator = GetComponentInParent<GraphNavigator>();
    }

    public void UpdateColor()
    {
        Color color = Node.Color;
        if (QUtils.GetLuminance(color) < .3f)
        {
            color += new Color(.5f, .6f, .4f);
        }
        grahpicColorDisplay.color = color;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        if (eventData.button == PointerEventData.InputButton.Left)
            graphNavigator.PreviewLine(this);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        if (eventData.button == PointerEventData.InputButton.Left)
            graphNavigator.TryConnectNode(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        graphNavigator.Hover();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        graphNavigator.Unhover();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        if (isDragging) return;

        if (eventData.button == PointerEventData.InputButton.Right)
            graphNavigator.DeleteNode(this);
    }
}
