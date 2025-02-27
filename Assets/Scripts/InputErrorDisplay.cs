using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_InputField))]
public class InputErrorDisplay : MonoBehaviour
{
    [Tooltip("Sets the tooltip text to error message. Can be null")]
    [SerializeField] private Tooltip tooltip;
    [SerializeField] private bool hideTooltip = true;
    private TMP_InputField inputField;

    public void DisplayError(string error)
    {
        inputField.image.color = Color.red;
        if (tooltip != null)
        {
            tooltip.text = error;
            if (hideTooltip) tooltip.gameObject.SetActive(true);
        }
    }
    /// <summary>
    /// Clears the InputField from any error
    /// </summary>
    public void Clear()
    {
        inputField.image.color = Color.white;
        if (tooltip != null && hideTooltip)
            tooltip.gameObject.SetActive(false);
    }

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        //inputField.onSelect.AddListener(str =>
        //{
        //    Clear();
        //});
        inputField.onValueChanged.AddListener(str =>
        {
            Clear();
        });
        //inputField.onTextSelection.AddListener((str, i, j) =>
        //{
        //    StartCoroutine(DelayedClear());
        //    //Clear();
        //});

        if (tooltip != null && hideTooltip)
            tooltip.gameObject.SetActive(false);
    }
    /// <summary>
    /// One can't update the image color onTextSelection in the same frame????????????
    /// </summary>
    private IEnumerator DelayedClear()
    {
        yield return null;
        Clear();
    }
}
