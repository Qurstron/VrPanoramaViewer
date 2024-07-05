using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public class NodeGrabTransformer : XRBaseGrabTransformer
{
    public float easingDuration = 1.0f;
    public float massInfuence = 0.0f;
    private Vector3 originalPos = Vector3.zero;
    private Sequence sequence = null;

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

        float massDuration = 0.0f;
        if (TryGetComponent<Rigidbody>(out var rigidbody))
        {
            massDuration = rigidbody.mass * massInfuence;
        }

        if (TryGetComponent<XRGrabInteractable>(out var grabInteractable))
        {
            grabInteractable.selectExited.AddListener((args) =>
            {
                //if (sequence == null)
                //{
                //    sequence = DOTween.Sequence();
                //    sequence.Complete();
                //}
                //if (!sequence.IsComplete())
                sequence.Kill(true);
                sequence = DOTween.Sequence();
                sequence.Append(transform.DOMove(originalPos, easingDuration + massDuration).SetEase(Ease.OutElastic));
                // fail safe
                sequence.onComplete = () =>
                {
                    transform.position = originalPos;
                };
            });

            grabInteractable.selectEntered.AddListener((args) =>
            {
                sequence.Kill(true);
                originalPos = transform.position;
            });
        }
        else
        {
            Debug.LogWarning("no XRGrabInteractable found!");
        }
    }
}
