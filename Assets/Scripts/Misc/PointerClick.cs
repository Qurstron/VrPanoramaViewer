using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class PointerClick : MonoBehaviour, IPointerClickHandler
{
    public PointerEventData.InputButton inputButton = PointerEventData.InputButton.Right;
    public UnityEvent onClick = new();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == inputButton)
        {
            onClick.Invoke();
        }
    }
}
