using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using UnityEngine;
using Application = UnityEngine.Application;

public class ViewController : MonoBehaviour
{
    public View startView;
    public bool closeAppOnEmptyBack = true;
    [SerializeField] private int topCanvasSortOrder = 10;
    [SerializeField] private int lastCanvasSortOrder = 1;
    private readonly Stack<View> viewStack = new();
    private View currentView = null;
    private Tweener fadebleTweenNew = null;
    private Tweener fadebleTweenOld = null;
    public View CurrentView
    {
        get { return currentView; }
    }

    void Start()
    {
        foreach (View view in GetComponentsInChildren<View>())
        {
            if (view == startView) continue;
            view.gameObject.SetActive(false);
        }

        DisplayView(startView);
    }

    public void DisplayView(View view, bool keepOldView = false)
    {
        if (currentView != null)
        {
            ChangeViews(currentView, view);
        }
        else
        {
            view.gameObject.SetActive(true);
        }

        if (!view.IsIntermediate) viewStack.Push(view);
        currentView = view;
    }
    public void Back()
    {
        if (currentView.IsIntermediate && viewStack.Count >= 1)
        {
            ChangeViews(currentView, viewStack.Peek());
            return;
        }
        if (viewStack.Count <= 1)
        {
            if (closeAppOnEmptyBack) Application.Quit();
            return;
        }

        ChangeViews(viewStack.Pop(), viewStack.Peek());

        return;
    }
    public bool Contains(View view)
    {
        return viewStack.Contains(view);
    }

    // TODO: refactor the if hell
    private void ChangeViews(View oldView, View newView)
    {
        fadebleTweenOld.Kill(true);
        fadebleTweenNew.Kill(true);

        oldView.Canvas.overrideSorting = true;
        newView.Canvas.overrideSorting = true;
        newView.Canvas.sortingOrder = lastCanvasSortOrder;

        if (oldView is FadeableView)
        {
            FadeableView fadeableView = oldView as FadeableView;
            CanvasGroup cg = fadeableView.GetComponent<CanvasGroup>();
            if (cg.alpha > 0)
            {
                fadebleTweenOld = DOVirtual.Float(cg.alpha, 0, fadeableView.easeOutTime * cg.alpha, f =>
                {
                    cg.alpha = f;
                }).SetEase(fadeableView.easeOutFade);
                fadebleTweenOld.onKill = () =>
                {
                    cg.alpha = 0;
                };
                fadebleTweenOld.onComplete = () =>
                {
                    oldView.OnHide.Invoke();
                    fadeableView.gameObject.SetActive(false);
                    Application.runInBackground = AppConfig.Config.runInBackground;
                    oldView.Canvas.sortingOrder = lastCanvasSortOrder;
                    newView.Canvas.sortingOrder = topCanvasSortOrder;
                };
            }
        }
        else
        {
            oldView.OnHide.Invoke();
            if (newView is not FadeableView && !newView.IsTransparent) oldView.gameObject.SetActive(false);
            //newView.transform.SetAsLastSibling();
            Application.runInBackground = AppConfig.Config.runInBackground;
            //oldView.Canvas.sortingOrder = lastCanvasSortOrder;
            newView.Canvas.sortingOrder = topCanvasSortOrder;
        }
        
        if (newView is FadeableView)
        {
            FadeableView fadeableView = newView as FadeableView;
            CanvasGroup cg = fadeableView.GetComponent<CanvasGroup>();
            cg.alpha = 0;
            fadebleTweenNew = DOVirtual.Float(0, 1, fadeableView.easeInTime, f =>
            {
                cg.alpha = f;
            }).SetEase(fadeableView.easeInFade);
            if (fadeableView.fullAlphaOnKill)
            {
                fadebleTweenNew.onKill = () =>
                {
                    cg.alpha = 1;
                };
            }
            fadebleTweenNew.onComplete = () =>
            {
                if (!newView.IsTransparent) oldView.gameObject.SetActive(false);
            };
        }
        newView.gameObject.SetActive(true);
        newView.OnShow.Invoke();

        if (newView.OverridingRunBackground)
            Application.runInBackground = true;
        currentView = newView;
    }
}
