using DG.Tweening;
using DG.Tweening.Core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingAnimation : MonoBehaviour
{
    public Image spinner;
    public TMP_Text text;
    public float speed = .5f;

    private Tweener tween;
    private bool isPlaying = false;

    private void Start()
    {
        spinner.enabled = false;
    }

    public void StartAnimation()
    {
        if (isPlaying) return;
        isPlaying = true;
        text.enabled = false;
        spinner.enabled = true;

        tween = spinner.transform.DOLocalRotate(new Vector3(0, 0, -180), speed).SetLoops(-1, LoopType.Incremental).SetEase(Ease.Linear);
    }
    public void StopAnimation()
    {
        if (!isPlaying) return;
        isPlaying = false;
        text.enabled = true;
        spinner.enabled = false;

        tween.Kill();
    }
    public void ErrorAnimation()
    {
        StopAnimation();
        text.DOColor(Color.red, .25f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.Linear);
    }
}
