using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

public class FineTuneGrabTransformer : XRBaseGrabTransformer
{
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

        // Compute the new target world pose
        if (grabInteractable.trackRotation)
        {
            // Transform that offset direction from world space to local space of the transform it's relative to.
            // It will be applied to the interactor's attach position using the orientation of the Interactor's attach transform.
            var positionOffset = thisAttachTransform.InverseTransformDirection(attachOffset);
            var rotationOffset = Quaternion.Inverse(Quaternion.Inverse(thisTransformPose.rotation) * thisAttachTransform.rotation);

            targetPose.position = (interactorAttachPose.rotation * positionOffset) + interactorAttachPose.position;
            //targetPose.rotation = (interactorAttachPose.rotation * rotationOffset);
            targetPose.rotation = Quaternion.Euler(0, (interactorAttachPose.rotation * rotationOffset).eulerAngles.y, 0);
            //targetPose.rotation *= Quaternion.Euler(0, interactorAttachPose.rotation.eulerAngles.y, 0);
        }
        else
        {
            // When not using the rotation of the Interactor, the world offset direction can be directly
            // added to the Interactor's attach transform position.
            targetPose.position = attachOffset + interactorAttachPose.position;
        }
    }
}
