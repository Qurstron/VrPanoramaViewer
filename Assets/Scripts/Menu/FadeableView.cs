using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class FadeableView : View
{
    [Header("Fade")]
    public Ease easeInFade = Ease.Linear;
    public Ease easeOutFade = Ease.Linear;
    public float easeInTime = 1f;
    public float easeOutTime = 1f;
    public bool fullAlphaOnKill = true;
}
