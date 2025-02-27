using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipManager : MonoBehaviour
{
    [SerializeField] private float timeToShow = 1;
    [SerializeField] private float tweenTime = 0.25f;
    private static TooltipManager tooltip = null;
    private static CanvasGroup canvasGroup = null;
    private static Tween alphaTween = null;

    public static void Show(string text)
    {
        if (tooltip.isActiveAndEnabled) Debug.LogWarning("multiple tooltips shown at once");
        tooltip.GetComponentInChildren<TMP_Text>().text = text;
        tooltip.Invoke(nameof(TriggerTooltip), tooltip.timeToShow);
    }
    public static void ShowInstant(string text)
    {
        tooltip.CancelInvoke(nameof(TriggerTooltip));
        tooltip.TriggerTooltip();

        //canvasGroup.alpha = 1;
        //tooltip.GetComponentInChildren<TMP_Text>().text = text;
        //tooltip.gameObject.SetActive(true);
    }
    public static void Hide()
    {
        if (!tooltip.isActiveAndEnabled)
        {
            tooltip.CancelInvoke(nameof(TriggerTooltip));
        }
        alphaTween?.Kill();

        canvasGroup.alpha = 0;
        tooltip.gameObject.SetActive(false);
    }
    public static void ResetTimer()
    {
        if (!tooltip.isActiveAndEnabled)
        {
            tooltip.CancelInvoke(nameof(TriggerTooltip));
            tooltip.Invoke(nameof(TriggerTooltip), tooltip.timeToShow);
        }
    }


    void Start()
    {
        if (tooltip != null) throw new Exception("There can't be 2 tooltip managers");

        tooltip = this;
        canvasGroup = GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    private void TriggerTooltip()
    {
        tooltip.gameObject.SetActive(true);
        alphaTween = DOVirtual.Float(0, 1, tweenTime, f =>
        {
            canvasGroup.alpha = f;
        }).SetEase(Ease.Linear);
    }
}
