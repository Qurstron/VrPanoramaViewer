using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TMP_Text))]
public class Typewriter : MonoBehaviour
{
    [TextArea(5, 10)]
    public string text;
    public float startDelay = 0f;
    public float typeDelay = 0.1f;
    public bool isUntyping = false;
    public bool startWithText = false;
    public bool useTmpText = false;
    public OnFinish onFinish;

    private TMP_Text tmpText;
    private int charIndex = 0;
    private Coroutine typeCorutine;
    private bool isStoped = false;

    private void Start()
    {
        tmpText = GetComponent<TMP_Text>();
        if (useTmpText) text = tmpText.text;
        tmpText.text = startWithText ? text : "";
    }

    public void TypeText(bool ignoreTypeDelay = false)
    {
        //if (isUntyping)
        //{
        //    Stop();
        //    isUntyping = false;
        //    if (IsFinished()) charIndex = 0;
        //    Type();

        //    return;
        //}
        isUntyping = false;

        if (charIndex > 0) return;
        Type(ignoreTypeDelay);
    }
    public void UntypeText(bool ignoreTypeDelay = false)
    {
        //if (!isUntyping)
        //{
        //    Stop();
        //    isUntyping = true;
        //    if (IsFinished()) charIndex = text.Length;
        //    Type();

        //    return;
        //}
        isUntyping = true;

        if (charIndex < text.Length - 1) return;
        Type(ignoreTypeDelay);
    }
    private void Type(bool ignoreTypeDelay)
    {
        isStoped = false;
        if (IsFinished()) return;

        if (isActiveAndEnabled)
            typeCorutine = StartCoroutine(TypeCoroutine(ignoreTypeDelay));
    }

    // reset cursor and text to the beginning (relative to typing direction)
    public void ResetText()
    {
        ResetCursor();
        if (isUntyping)
        {
            tmpText.text = text;
        }
        else
        {
            tmpText.text = "";
        }
    }
    // Only reset cursor to the beginning (relative to typing direction)
    public void ResetCursor()
    {
        Stop();
        if (isUntyping)
        {
            charIndex = text.Length - 1;
        }
        else
        {
            charIndex = 0;
        }
    }
    public void Skip()
    {
        if (typeCorutine == null) return;

        StopCoroutine(typeCorutine);
        charIndex = text.Length - 1;
        tmpText.text = text;
    }
    public void Stop()
    {
        if (typeCorutine == null) return;

        StopCoroutine(typeCorutine);
        isStoped = true;
    }
    public void Resume()
    {
        if (!isStoped) return;
        Type(true);
    }

    public bool IsFinished()
    {
        return isUntyping ? charIndex == 0 : charIndex == text.Length - 1;
    }

    private IEnumerator TypeCoroutine(bool ignoreTypeDelay)
    {
        if (!ignoreTypeDelay)
            yield return new WaitForSeconds(startDelay);

        int typeDirection = isUntyping ? -1 : 1;
        var wait = new WaitForSeconds(typeDelay);
        // TODO: support rich text
        for (; charIndex < text.Length && charIndex >= 0; charIndex += typeDirection)
        {
            tmpText.text = text.Substring(0, charIndex + 1);
            yield return wait;
        }
        if (isUntyping) tmpText.text = "";

        onFinish.Invoke();
    }
}

[Serializable]
public class OnFinish : UnityEvent { }
