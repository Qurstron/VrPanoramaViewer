using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using static JSONClasses;

[RequireComponent(typeof(Collider))]
public class FloppyDrive : MonoBehaviour
{
    public FileManager fileManager;
    public StateControler stateControler;

    [Header("Floppy")]
    public Transform floppyStart;
    public Transform floppyEnd;
    public float speed = 1.0f;
    public Ease ease = Ease.Linear;
    public bool allowDropInsert = false;

    private FloppyDisk insertedFloppy = null;
    private Tween floppyTween = null;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Floppy")) return;
        FloppyDisk floppy = other.GetComponent<FloppyDisk>();

        if (!floppy.grabInteractable.isSelected && allowDropInsert)
        {
            GrabFloppy(floppy);
            return;
        }

        floppy.grabInteractable.selectExited.AddListener(OnSelectExit);
    }
    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Floppy")) return;
        FloppyDisk floppy = other.GetComponent<FloppyDisk>();

        floppy.grabInteractable.selectExited.RemoveListener(OnSelectExit);
    }

    private void OnSelectExit(SelectExitEventArgs args)
    {
        FloppyDisk floppy = args.interactableObject.transform.GetComponent<FloppyDisk>();
        GrabFloppy(floppy);
    }
    private void GrabFloppy(FloppyDisk floppy)
    {
        if (insertedFloppy != null) return;
        insertedFloppy = floppy;
        var task = fileManager.LoadLocalPanorama(floppy.PanoramaName);
        
        insertedFloppy.GetComponent<Collider>().enabled = false;
        insertedFloppy.GetComponent<Rigidbody>().isKinematic = true;

        insertedFloppy.transform.SetPositionAndRotation(floppyStart.position, floppyStart.rotation);
        floppyTween = insertedFloppy.transform.DOMove(floppyEnd.position, speed).SetEase(ease);
        floppyTween.onComplete = async () =>
        {
            // TODO: Display loading
            Config config =  await task;
            if (config == null)
            {
                // TODO: Display Error
                return;
            }
            stateControler.ToggleMenu();
        };
    }
    private void EjectFloppy()
    {
        if (insertedFloppy == null) return;

        floppyTween.Rewind();
        insertedFloppy.GetComponent<Collider>().enabled = true;
        insertedFloppy.GetComponent<Rigidbody>().isKinematic = false;
    }
}
