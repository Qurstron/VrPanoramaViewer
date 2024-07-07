using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class Typewriter : MonoBehaviour
{
    [TextArea(5, 10)]
    public string text;
    public float startDelay = 0f;
    public float typeDelay = 0.1f;

    private TMP_Text tmpText;
    private int charIndex = 0;
    private Coroutine typeCorutine;

    private void Start()
    {
        tmpText = GetComponent<TMP_Text>();
    }

    public void TypeText()
    {
        if (charIndex > 0) return;
        typeCorutine = StartCoroutine(TypeCoroutine());
    }
    public void ResetText()
    {
        if (typeCorutine == null) return;

        StopCoroutine(typeCorutine);
        charIndex = 0;
        tmpText.text = "";
    }
    public void Skip()
    {
        if (typeCorutine == null) return;

        StopCoroutine(typeCorutine);
        charIndex = text.Length - 1;
        tmpText.text = text;
    }
    public bool IsFinished()
    {
        return charIndex == text.Length - 1;
    }

    private IEnumerator TypeCoroutine()
    {
        yield return new WaitForSeconds(startDelay);

        // TODO: support rich text
        var wait = new WaitForSeconds(typeDelay);
        for (; charIndex < text.Length; charIndex++)
        {
            tmpText.text += text[charIndex];
            yield return wait;
        }
        //foreach (char c in text)
        //{
        //    tmpText.text += c;
        //    yield return wait;
        //}
    }
}
