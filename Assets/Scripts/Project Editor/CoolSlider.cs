using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Vector2 = UnityEngine.Vector2;

[Obsolete("Use Slider instead", false)]
public class CoolSlider : DragableChange, IDragHandler, IScrollHandler
{
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private float value = 0;
    [SerializeField] private float speed = 1;
    [SerializeField] private string text;
    [SerializeField] private int padLeft = 0;
    [SerializeField] private int precision = 1;
    [SerializeField] private float step = 0;
    [SerializeField] private float min = 0;
    [SerializeField] private float max = 0;

    public UnityEvent<float> onValueChanged = new();

    private readonly NumberFormatInfo setPrecision = new();
    private float subStepOffset = 0;

    protected override void Start()
    {
        base.Start();
        if (displayText == null)
        {
            displayText = GetComponent<TMP_Text>();
            if (displayText == null) throw new Exception("displayText not found");
        }

        //originalText = displayText.text;
        setPrecision.NumberDecimalDigits = precision;
    }

    protected new void OnValidate()
    {
        //displayText.text = text + value;
        setPrecision.NumberDecimalDigits = precision;
        OnDeltaDrag(Vector2.zero, null);
    }

    public override void OnDeltaDrag(Vector2 deltaPos, PointerEventData eventData)
    {
        float delta = deltaPos.x * speed + subStepOffset;
        float n = MathF.Round(delta / step, MidpointRounding.AwayFromZero) * step;
        subStepOffset = delta - n;
        value += n;
        value = Mathf.Clamp(value, min, max);

        UpdateUI();
        onValueChanged.Invoke(value);
    }
    public void OnScroll(PointerEventData eventData)
    {
        value += eventData.scrollDelta.y * step;
        value = Mathf.Clamp(value, min, max);

        UpdateUI();
        onValueChanged.Invoke(value);
    }

    private void UpdateUI()
    {
        displayText.text = text + value.ToString("N", setPrecision).PadLeft(padLeft);
    }

    public void SetValueDirect(float value)
    {
        this.value = value;
        UpdateUI();
    }
}
