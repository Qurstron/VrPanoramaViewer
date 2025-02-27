using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ColorPickerHandler : MonoBehaviour
{
    [SerializeField] private Image colorDisplay;
    [SerializeField] private TMP_InputField hexDisplay;
    private bool useAlpha = false;

    private static readonly List<ColorSpace> colorSpaces = new();
    private static ColorPickerHandler instance;
    private static ColorPicker colorPicker;

    private void Awake()
    {
        if (instance != null) throw new Exception("There can't be 2 tooltip managers");

        SetupDefaultColorSpaces();
        SetupListener();

        instance = this;
        gameObject.SetActive(false);
    }
    private void SetupDefaultColorSpaces()
    {
        ColorSpace rgb = new()
        {
            name = "RGB",
            sliders = new string[] { "r", "g", "b" },
            calcFormula = values => new Color(values[0], values[1], values[2])
        };
        ColorSpace hsv = new()
        {
            name = "HSV",
            sliders = new string[] { "h", "s", "v" },
            calcFormula = values => Color.HSVToRGB(values[0], values[1], values[2])
        };
        colorSpaces.Add(rgb);
        colorSpaces.Add(hsv);
    }
    private void SetupListener()
    {
        hexDisplay.onEndEdit.AddListener(value =>
        {
            value = QUtils.FormatHexColor(value, QUtils.defaultColorHexFlags | (useAlpha ? 0 : QUtils.ColorHexFlags.NoAlpha));
            hexDisplay.text = value;

            try
            {
                colorPicker.onValueChanged.Invoke(QUtils.StringToColor(value));
            }
            catch
            {
                // Invalid Color, revoke to original
                hexDisplay.text = QUtils.FormatHexColor(colorPicker.Color);
            }
        });
    }

    public static void Pick(ColorPicker picker)
    {
        colorPicker = picker;
        instance.gameObject.SetActive(true);
        QUtils.CreateUIBlocker(instance.GetComponent<Canvas>(), blocker =>
        {
            Destroy(blocker);
            instance.gameObject.SetActive(false);
        });
    }
    public static void AddColorSpace(ColorSpace colorSpace)
    {
        colorSpaces.Add(colorSpace);
    }

    public class ColorSpace
    {
        public string name;
        public string[] sliders;
        public Func<float[], Color> calcFormula;
    }
}
