using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class NCParentEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    /// <summary>
    /// True on hover enter. False on hover exit.
    /// </summary>
    public UnityEvent<bool> OnHoverChange;
    public bool isActualParent = false;
    public int index = -1;
    public int parentIndex = -1;

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverChange.Invoke(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverChange.Invoke(false);
    }
}
