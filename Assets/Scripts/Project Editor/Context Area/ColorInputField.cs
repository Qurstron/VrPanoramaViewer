using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static QUtils;

[RequireComponent(typeof(ColorPicker))]
public class ColorInputField : ConfigActor
{
    [SerializeField] private ConfigField<string> configField;
    ColorPicker colorPicker;
    protected override void OnContextChange()
    {
        if (configField == null)
        {
            Debug.LogError("ConfigField is null");
            return;
        }

        GetUpdateEvent(configField.UpdateLevel).AddListener(() =>
        {
            if (colorPicker == null) return;
            if (configField.IsInputReady(Context))
            {
                string value = configField.GetField(Context);
                if (value == null) return; // Unset field, leave the inputField empty
                colorPicker.SetColorWithoutNotify(StringToColor(value));
            }
            else
            {
                colorPicker.SetColorWithoutNotify(null);
            }
        });
        configField.onFieldChange.AddListener(str =>
        {
            //inputField.text = str.ToString();
            colorPicker.SetColorWithoutNotify(StringToColor(str));
        });
    }

    private void Awake()
    {
        colorPicker = GetComponent<ColorPicker>();
        colorPicker.onValueChanged.AddListener(value =>
        {
            if (configField is IDynamicField<string>)
            {
                (configField as IDynamicField<string>).SetFieldDynamic(Context, value.ToHexString());
            }
        });
        colorPicker.onValueSelected.AddListener(value =>
        {
            string hex = QUtils.FormatHexColor(value, ColorHexFlags.LeadingHashtag);
            // No failureCallback has been set because the ToHexString() method can only produce correct strings
            Context.editor.ExecuteCommand(new ChangeValueCommand<string>(value.ToHexString(), configField, str => { }));
        });
    }
}
