using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Color = UnityEngine.Color;

[RequireComponent(typeof(Image))]
public class ColorPicker : MonoBehaviour
{
    //[SerializeField] private Color color = Color.white;
    [SerializeField] private TMP_Text displayHex;
    private Image image;
    public UnityEvent<Color> onValueChanged = new();
    public UnityEvent<Color> onValueSelected = new();
    public Color Color
    {
        get { return image.color; }
        set
        {
            SetColorWithoutNotify(value);
            onValueChanged.Invoke(value);
        }
    }

    public void SetColorWithoutNotify(Color? color)
    {
        if (color == null)
        {
            image.color = Color.gray;
            if (displayHex != null)
            {
                displayHex.text = "------";
                displayHex.color = Color.white;
            }
            return;
        }

        image.color = (Color)color;
        if (displayHex != null)
        {
            // remove the last 2 chars, because alpha is not relevant
            string hex = ((Color)color).ToHexString();
            displayHex.text = $"#{hex[..^2]}";

            //luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            // https://stackoverflow.com/questions/3942878/how-to-decide-font-color-in-white-or-black-depending-on-background-color
            if (QUtils.GetLuminance((Color)color) > 0.179) displayHex.color = Color.black;
            else displayHex.color = Color.white;
        }
    }
    public void Pick()
    {
        //ColorPickerHandler.Pick(this);
        EasyColor.ColorPicker.Create(image.color, "Node Color", color => onValueChanged.Invoke(color), color => onValueSelected.Invoke(color), false);
    }

    private void Awake()
    {
        image = GetComponent<Image>();
        onValueChanged.AddListener(color =>
        {
            SetColorWithoutNotify(color);
        });
        //onValueChanged.AddListener(color =>
        //{
        //    SetColorWithoutNotify(color);
        //});
    }
}
