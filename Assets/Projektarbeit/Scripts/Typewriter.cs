using System;
using System.Collections;
using System.Collections.Generic;
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
    public OnFinish onFinish;

    private TMP_Text tmpText;
    private int charIndex = 0;
    private Coroutine typeCorutine;
    private bool isStoped = false;

    private void Start()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    public void TypeText()
    {
        if (isUntyping)
        {
            Stop();
            isUntyping = false;
            if (IsFinished()) return;
            Type();

            return;
        }

        if (charIndex > 0) return;
        Type();
    }
    public void UntypeText()
    {
        if (!isUntyping)
        {
            Stop();
            isUntyping = true;
            if (IsFinished()) return;
            Type();

            return;
        }

        if (charIndex < text.Length - 1) return;
        Type();
    }
    private void Type()
    {
        isStoped = false;
        if (IsFinished()) return;

        typeCorutine = StartCoroutine(TypeCoroutine());
    }

    public void ResetText()
    {
        Stop();
        if (isUntyping)
        {
            charIndex = text.Length - 1;
            tmpText.text = text;
        }
        else
        {
            charIndex = 0;
            tmpText.text = "";
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
        Type();
    }

    public bool IsFinished()
    {
        return isUntyping ? charIndex == 0 : charIndex == text.Length - 1;
    }

    private IEnumerator TypeCoroutine()
    {
        yield return new WaitForSeconds(startDelay);

        int typeDirection = isUntyping ? -1 : 1;
        var wait = new WaitForSeconds(typeDelay);
        // TODO: support rich text
        for (; charIndex < text.Length; charIndex += typeDirection)
        {
            tmpText.text = text.Substring(0, charIndex + 1);
            yield return wait;
        }

        onFinish.Invoke();
    }
}

[Serializable]
public class OnFinish : UnityEvent { }
