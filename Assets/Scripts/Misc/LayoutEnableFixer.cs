using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rebuilds the layout OnEnable after 1 frame.
/// This can be necessary because if the layout is filled and enabled on the same frame 
/// </summary>
/// <remarks>
/// When stacking dynamic content use parent controlled size with layout element as child
/// </remarks>
[Obsolete]
public class LayoutEnableFixer : MonoBehaviour
{
    private void OnEnable()
    {
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
        StartCoroutine(UpdateLayoutGroup());
    }

    private IEnumerator UpdateLayoutGroup()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
    }
}
