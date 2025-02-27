using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(TMP_Dropdown))]
public class MenubarDropdown : MonoBehaviour
{
    [SerializeField] private List<DropdownEntry> options;
    private TMP_Dropdown dropdown;

    private void Start()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.options = options.Select(o => new TMP_Dropdown.OptionData(o.value)).ToList();
        dropdown.onValueChanged.AddListener(OnValueChanged);
        dropdown.SetValueWithoutNotify(-1);
    }
    private void OnValueChanged(int value)
    {
        options[value].onSelect.Invoke();
        // TMP_Dropdown are designed to select a value rather than to chose
        // we need to set the value to an unreachable one, so onValueChanged is always called on click
        dropdown.SetValueWithoutNotify(-1);
    }
}

[Serializable]
public class DropdownEntry
{
    public string value;
    public UnityEvent onSelect;
}
