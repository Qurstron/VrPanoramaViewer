using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

[RequireComponent(typeof(NodeProperties))]
public class NodeGrabTransformer : XRBaseGrabTransformer
{
    public float easingDuration = 1.0f;
    public float massInfuence = 0.0f;
    public OnAnimationFinish onAnimationFinish;
    public OnHoverExit onHoverExitNoSelect;

    private NodeProperties nodeProperties;
    private Sequence sequence = null;
    private bool isHolding = false;

    public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
    {
        switch (updatePhase)
        {
            case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
            case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
            {
                UpdateTarget(grabInteractable, ref targetPose);

                break;
            }
        }
    }
    internal static void UpdateTarget(XRGrabInteractable grabInteractable, ref Pose targetPose)
    {
        var interactor = grabInteractable.interactorsSelecting[0];
        var interactorAttachPose = interactor.GetAttachTransform(grabInteractable).GetWorldPose();
        var thisTransformPose = grabInteractable.transform.GetWorldPose();
        var thisAttachTransform = grabInteractable.GetAttachTransform(interactor);

        // Calculate offset of the grab interactable's position relative to its attach transform
        var attachOffset = thisTransformPose.position - thisAttachTransform.position;
        targetPose.position = attachOffset + interactorAttachPose.position;
    }

    private new void Start()
    {
        base.Start();
        nodeProperties = GetComponent<NodeProperties>();

        float massDuration = 0.0f;
        if (TryGetComponent<Rigidbody>(out var rigidbody))
        {
            massDuration = rigidbody.mass * massInfuence;
        }

        if (TryGetComponent<XRGrabInteractable>(out var grabInteractable))
        {
            grabInteractable.selectExited.AddListener((args) =>
            {
                sequence.Kill(true);
                sequence = DOTween.Sequence();
                sequence.Append(transform.DOMove(nodeProperties.originalPos, easingDuration + massDuration).SetEase(Ease.OutElastic));
                sequence.onComplete = () =>
                {
                    transform.position = nodeProperties.originalPos;
                    onAnimationFinish.Invoke();
                };

                isHolding = false;
                onHoverExitNoSelect.Invoke();
            });

            grabInteractable.selectEntered.AddListener((args) =>
            {
                sequence.Kill(true);
                nodeProperties.originalPos = transform.position;
                isHolding = true;
            });

            grabInteractable.hoverExited.AddListener((args) =>
            {
                if (isHolding) return;
                onHoverExitNoSelect.Invoke();
            });
        }
        else
        {
            Debug.LogWarning("no XRGrabInteractable found!");
        }
    }
}

[Serializable]
public class OnAnimationFinish : UnityEvent { }
[Serializable]
public class OnHoverExit : UnityEvent { }
