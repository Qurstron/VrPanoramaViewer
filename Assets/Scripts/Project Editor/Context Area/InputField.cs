using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_InputField))]
public abstract class InputField<T> : ConfigActor, IScrollHandler
{
    [SerializeField] protected ConfigField<T> configField;
    [SerializeField] protected bool captureScroll = false;
    [SerializeField] protected bool submitOnEnter = false;
    [Tooltip("Delets the input text when the configField is unready to recive input")]
    [SerializeField] protected bool obscureInputOnUnready = false;
    protected TMP_InputField inputField;
    protected readonly string obscureStr = "--";

    protected override void OnContextChange()
    {
        if (configField == null)
        {
            Debug.LogError("ConfigField is null");
            return;
        }

        GetUpdateEvent(configField.UpdateLevel).AddListener(() =>
        {
            if (inputField == null) inputField = GetComponent<TMP_InputField>();

            if (configField.IsInputReady(Context))
            {
                T value = configField.GetField(Context);
                // Unset field, leave the inputField empty
                //if (value == null)
                //{
                //    inputField.SetTextWithoutNotify("");
                //    return;
                //}

                inputField.interactable = true;
                if (value == null) inputField.SetTextWithoutNotify("");
                else inputField.SetTextWithoutNotify(value.ToString());
                inputField.onEndEdit.Invoke(inputField.text);
            }
            else
            {
                inputField.interactable = false;
                if (obscureInputOnUnready) inputField.SetTextWithoutNotify(obscureStr);
            }
        });
        configField.onFieldChange.AddListener(str =>
        {
            if (str == null) inputField.SetTextWithoutNotify(null);
            else inputField.SetTextWithoutNotify(str.ToString());
        });
    }

    protected virtual void Awake()
    {
        if (configField == null) throw new Exception($"missing configField in .../{transform.parent.name}/{name}");

        inputField = GetComponent<TMP_InputField>();
        inputField.onValueChanged.AddListener(ValueChange);
        inputField.onDeselect.AddListener(Submit);
        if (submitOnEnter) inputField.onSubmit.AddListener(Submit);
    }

    public virtual void Submit(string str)
    {
        // To set the inputField with the value it was called from may seem odd but
        // it allows to override this method and modify str before submiting
        inputField.SetTextWithoutNotify(str);
        SubmitValue((T)Convert.ChangeType(str, typeof(T)));
    }
    public virtual void SubmitValue(T value)
    {
        Context.editor.ExecuteCommand(new ChangeValueCommand<T>(value, configField, OnFailedInput));
    }
    public virtual void ValueChange(string str)
    {
        //configField.onFieldChange.Invoke((T)Convert.ChangeType(str, typeof(T)));
        if (configField is IDynamicField<T>)
        {
            (configField as IDynamicField<T>).SetFieldDynamic(Context, (T)Convert.ChangeType(str, typeof(T)));
        }
    }

    protected void OnFailedInput(string reason)
    {
        // TODO: reflect failure reason to user
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (captureScroll) return;

        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.OnScroll(eventData);
        }
    }
}
