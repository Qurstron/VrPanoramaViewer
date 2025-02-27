using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class CustomEventEmitter : MonoBehaviour, IPointerUpHandler
{
    public UnityEvent<PointerEventData> onPointerUp;

    public void OnPointerUp(PointerEventData eventData)
    {
        onPointerUp.Invoke(eventData);
    }
}
