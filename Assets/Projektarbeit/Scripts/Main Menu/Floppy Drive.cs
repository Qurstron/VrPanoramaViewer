using DG.Tweening;
using JSONClasses;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Collider))]
public class FloppyDrive : MonoBehaviour
{
    public FileManager fileManager;
    public StateControler stateControler;
    public GraphUI graphUI;
    public PanoramaSphereController panoramaSphereController;

    [Header("Floppy")]
    public Transform floppyStart;
    public Transform floppyEnd;
    public float speed = 1.0f;
    public Ease ease = Ease.Linear;
    public bool allowDropInsert = false;
    public Color ledColor = Color.green;
    public float ledSpeed = 1;

    private FloppyDisk insertedFloppy = null;
    private Tween floppyTween = null;
    private TaskScheduler scheduler;
    // led specific
    private bool isLedOn = true;
    private bool isLoaded = false;
    private bool isAnimationFinished = false;
    private MaterialPropertyBlock propertyBlock = null;
    private Coroutine ledCoroutine = null;
    private Task<Config> loaderTask = null;

    private void Start()
    {
        scheduler = TaskScheduler.FromCurrentSynchronizationContext();
        propertyBlock = new MaterialPropertyBlock();

        GetComponent<MeshRenderer>().SetPropertyBlock(propertyBlock);
        ledCoroutine = StartCoroutine(LedBlink());
    }
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

        //loaderTask = Task.Run(() => fileManager.LoadLocalPanorama(fileManager.GetFolderPath(floppy.PanoramaName)));
        loaderTask = Task.Run(() => fileManager.LoadLocalPanorama(floppy.entry.path));
        loaderTask.ContinueWith(OnFloppyLoaded, scheduler);

        insertedFloppy.GetComponent<Collider>().enabled = false;
        insertedFloppy.GetComponent<Rigidbody>().isKinematic = true;

        isAnimationFinished = false;
        insertedFloppy.transform.SetPositionAndRotation(floppyStart.position, floppyStart.rotation);
        floppyTween = insertedFloppy.transform.DOMove(floppyEnd.position, speed).SetEase(ease);
        floppyTween.onComplete = () =>
        {
            isAnimationFinished = true;
            if (isLoaded) return;

            StopCoroutine(ledCoroutine);
            propertyBlock.SetColor("_Color", Color.cyan);

            // TODO: Display loading
        };
    }
    private void EjectFloppy()
    {
        if (insertedFloppy == null) return;
        isLoaded = false;

        floppyTween.Rewind();
        insertedFloppy.GetComponent<Collider>().enabled = true;
        insertedFloppy.GetComponent<Rigidbody>().isKinematic = false;
    }

    private void OnFloppyLoaded(Task<Config> task)
    {
        Config config = task.Result;
        isLoaded = true;
        //graphUI.SetConfig(config);
        //panoramaSphereController.config = config;

        if (config == null)
        {
            // TODO: Display Error
            propertyBlock.SetColor("_Color", Color.red);
            return;
        }

        propertyBlock.SetColor("_Color", ledColor);
        StartCoroutine(Switch());
    }
    private bool Pain()
    {
        return isAnimationFinished;
    }
    private IEnumerator Switch()
    {
        yield return new WaitUntil(Pain);

        //graphUI.InitGraph();
        stateControler.ToggleMenu();
    }
    private IEnumerator LedBlink()
    {
        var waitSec = new WaitForSeconds(ledSpeed);

        while (true)
        {
            if (isLedOn = !isLedOn)
            {
                propertyBlock.SetColor("_Color", ledColor);
                yield return waitSec;
            }

            propertyBlock.SetColor("_Color", Color.black);
            yield return waitSec;
        }
    }
}
