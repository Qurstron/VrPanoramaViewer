using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Slider : DragableChange, IScrollHandler
{
    [Tooltip("Linked InputField, can be null")]
    [SerializeField] private TMP_InputField inputField;
    [Tooltip("Value Display, can be null")]
    [SerializeField] private TMP_Text displayText;
    [Header("Behavior")]
    [SerializeField] private float value = 0;
    [Tooltip("Speed of mouse drag")]
    [SerializeField] private float speed = 1;
    [Tooltip("Set to 0 to disable Mousewheel input")]
    [SerializeField] private float mouseWheelSpeed = 1;
    [SerializeField] private float step = 0;
    [SerializeField] private float min = 0;
    [SerializeField] private float max = 0;
    [Tooltip("Scales the drag and mouse speed with the range max - min")]
    [SerializeField] private bool scaleWithRange = false;
    [Header("Formating")]
    [SerializeField] private string text;
    [SerializeField] private int padLeft = 0;
    [SerializeField] private int precision = 1;

    public UnityEvent<float> onValueChanged = new();
    public UnityEvent<float> onSubmit = new();

    private readonly NumberFormatInfo setPrecision = new();
    /// <summary>
    /// When sliding the actual input can land between a step
    /// to make a smooth slide we save the value and add it later
    /// </summary>
    private float subStepOffset = 0;
    private float range;

    public float Value
    {
        get { return value; }
        set
        {
            this.value = value;
            setPrecision.NumberDecimalDigits = precision;

            if (displayText != null) displayText.text = text + GetValueString();
            if (inputField != null) inputField.text = GetValueString();
            onValueChanged.Invoke(value);
        }
    }

    public override void OnDeltaDrag(Vector2 deltaPos, PointerEventData eventData)
    {
        float delta = deltaPos.x * speed;
        CalcValue(delta);
    }
    public override void OnRelease()
    {
        onSubmit.Invoke(value);
    }

    public void OnScroll(PointerEventData eventData)
    {
        float delta = eventData.scrollDelta.y * ((step == 0) ? 1 : step) * mouseWheelSpeed;
        CalcValue(delta);
    }
    public void SetValueWithoutNotify(float value)
    {
        this.value = value;
        if (displayText != null) displayText.text = text + GetValueString();
        if (inputField != null) inputField.text = GetValueString();
    }

    private void CalcValue(float delta)
    {
        if (!inputField.IsInteractable()) return;

        if (scaleWithRange) delta *= range;
        delta += subStepOffset;

        float n = delta;
        if (step != 0)
        {
            n = MathF.Round(delta / step, MidpointRounding.AwayFromZero) * step;
            subStepOffset = delta - n;
        }
        value += n;
        if (min != max) Value = Mathf.Clamp(value, min, max);
        else Value = value;
    }
    private string GetValueString()
    {
        return value.ToString("N", setPrecision).PadLeft(padLeft);
    }

    private new void Awake()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(input =>
            {
                if (float.TryParse(input, out float f))
                {
                    Value = f;
                }
            });
        }

        range = max - min;
    }
    //private new void OnValidate()
    //{
    //    setPrecision.NumberDecimalDigits = precision;
    //    OnDeltaDrag(Vector2.zero, null);
    //}
}
