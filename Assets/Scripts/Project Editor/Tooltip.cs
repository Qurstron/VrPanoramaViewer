using UnityEngine;
using UnityEngine.EventSystems;

public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerClickHandler
{
    [TextArea(3, 10)]
    public string text;
    [SerializeField] private bool triggerOnMouse = false;
    [SerializeField] private bool hideOnMove = false;

    private bool wasTriggeredByMouse = false;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!triggerOnMouse) return;

        if (wasTriggeredByMouse)
        {
            TooltipManager.Hide();
            wasTriggeredByMouse = false;
            return;
        }

        wasTriggeredByMouse = true;
        TooltipManager.ShowInstant(text);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (wasTriggeredByMouse) return;
        TooltipManager.Show(text);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        wasTriggeredByMouse = false;
        TooltipManager.Hide();
    }
    public void OnPointerMove(PointerEventData eventData)
    {
        if (hideOnMove)
        {
            TooltipManager.Hide();
            TooltipManager.Show(text);
        }
        else
        {
            TooltipManager.ResetTimer();
        }
    }

    private void OnDestroy()
    {
        TooltipManager.Hide();
    }
}
