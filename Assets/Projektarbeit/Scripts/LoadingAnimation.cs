using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LoadingAnimation : MonoBehaviour
{
    //public Transform shell;
    //public Transform triangle;
    //public Transform fins;
    //public Transform imageParent;
    public Graphic existingGrphic;
    public Ease ease = Ease.InOutSine;
    public float speed = .5f;
    public int rotationsPerAnimation = 1;

    private Transform imageParent;
    private Transform shell;
    private Transform triangle;
    private Transform fins;
    private DG.Tweening.Sequence tween;
    private bool isPlaying = false;

    private void Start()
    {
        imageParent = transform.GetChild(0);
        shell = imageParent.GetChild(0);
        triangle = imageParent.GetChild(1);
        fins = imageParent.GetChild(2);

        imageParent.gameObject.SetActive(false);
        //StartAnimation();
    }

    public void StartAnimation()
    {
        if (isPlaying) return;
        SetAnimationStates(true);
        //isPlaying = true;
        //existingGrphic.enabled = false;
        //spinner.enabled = true;

        float angle = 60 * rotationsPerAnimation; // 60° because the icon is 3/6 way radial symetric
        tween = DOTween.Sequence();

        tween.Append(shell.DOLocalRotate(new Vector3(0, 0, angle), speed));
        tween.Join(triangle.DOLocalRotate(new Vector3(0, 0, -angle), speed));
        tween.Join(fins.DOLocalRotate(new Vector3(0, 0, angle), speed));
        tween.SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);

        //triangle.DOLocalRotate(new Vector3(0, 0, 60), speed).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
        //fins.DOLocalRotate(new Vector3(0, 0, -60), speed).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);

        //tween = spinner.transform.DOLocalRotate(new Vector3(0, 0, -180), speed).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
    }
    public void StopAnimation()
    {
        if (!isPlaying) return;
        SetAnimationStates(false);

        shell.localRotation = Quaternion.identity;
        triangle.localRotation = Quaternion.identity;
        fins.localRotation = Quaternion.identity;

        tween.Kill();
    }
    public void ErrorAnimation()
    {
        StopAnimation();
        existingGrphic.DOColor(Color.red, .25f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.Linear);
    }

    private void SetAnimationStates(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        if (existingGrphic != null) existingGrphic.enabled = !isPlaying;
        //spinner.enabled = isPlaying;
        imageParent.gameObject.SetActive(isPlaying);
    }
}
