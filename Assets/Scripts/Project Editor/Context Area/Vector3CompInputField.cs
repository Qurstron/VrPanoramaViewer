using TMPro;
using UnityEngine;

public class Vector3CompInputField : InputField<Vector3>
{
    [Tooltip("Index of the component of the vector")]
    [SerializeField] private int index;

    protected override void OnContextChange()
    {
        GetUpdateEvent(configField.UpdateLevel).AddListener(() =>
        {
            float value = configField.GetField(Context)[index];
            //if (value == null) return; // Unset field, leave the inputField empty
            if (inputField == null) inputField = GetComponent<TMP_InputField>();

            inputField.text = value.ToString();
            inputField.onEndEdit.Invoke(inputField.text);

            if (configField.IsInputReady(Context)) inputField.interactable = true;
            else
            {
                inputField.interactable = false;
                if (obscureInputOnUnready) inputField.SetTextWithoutNotify(obscureStr);
            }
        });
        configField.onFieldChange.AddListener(vec2 =>
        {
            inputField.SetTextWithoutNotify(vec2[index].ToString());
        });
    }
    public override void Submit(string str)
    {
        Submit(float.Parse(str));
    }
    public override void SubmitValue(Vector3 value)
    {
        Context.editor.ExecuteCommand(new ChangeValueCommand<Vector3>(value, configField, OnFailedInput));
    }
    public void Submit (float f)
    {
        Vector3 newValue = configField.GetField(Context);
        newValue[index] = f;
        SubmitValue(newValue);
    }

    public override void ValueChange(string str)
    {
        ValueChange(float.Parse(str));
    }
    public void ValueChange(float f)
    {
        Vector3 newValue = configField.GetField(Context);
        newValue[index] = f;
        if (configField is IDynamicField<Vector3>)
        {
            (configField as IDynamicField<Vector3>).SetFieldDynamic(Context, newValue);
        }
        //configField.onFieldChange.Invoke(newValue);
    }
}
