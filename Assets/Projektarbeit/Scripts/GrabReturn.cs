using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(XRGrabInteractable))]
public class GrabReturn : MonoBehaviour
{
    [Tooltip("leave empty for self")]
    public Transform target = null;
    public float easingTime = 1f;
    public Ease easing = Ease.OutSine;
    public bool returnToPosition = true;
    public bool returnToRotation = true;
    public bool returnToScale = true;
    public OnStartReturn onStartReturn;
    public OnFinishReturn onFinishReturn;

    private XRGrabInteractable grabInteractable;
    private Vector3 originalPos = Vector3.zero;
    private Quaternion originalRotation = Quaternion.identity;
    private Vector3 originalScale = Vector3.one;
    private Sequence sequence;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        if (target == null) target = transform;

        grabInteractable.selectEntered.AddListener((args) =>
        {
            sequence.Kill();
            originalPos = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
        });

        grabInteractable.selectExited.AddListener((args) =>
        {
            sequence = DOTween.Sequence();
            Tween lastTween = null;

            if (returnToPosition)
            {
                sequence.Join(lastTween = target.DOMove(originalPos, easingTime).SetEase(easing));
            }
            if (returnToRotation)
            {
                sequence.Join(lastTween = target.DORotateQuaternion(originalRotation, easingTime).SetEase(easing));
            }
            if (returnToScale)
            {
                sequence.Join(lastTween = target.DOScale(originalScale, easingTime).SetEase(easing));
            }

            if (lastTween != null)
            {
                lastTween.onComplete = () =>
                {
                    onFinishReturn.Invoke();
                };
            }

            onStartReturn.Invoke(sequence);
        });
    }

    [Serializable]
    public class OnStartReturn : UnityEvent<Sequence> { }
    [Serializable]
    public class OnFinishReturn : UnityEvent { }
}
