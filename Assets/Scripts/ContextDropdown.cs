using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ContextDropdown : Button
{
    [SerializeField] private GameObject dropdown;
    [SerializeField] private GameObject template;
    [SerializeField] private GameObject templateDivider;
    [SerializeField] private List<MenubarListEntry> entries;
    [SerializeField] private bool isOpen = false;
    private bool isHovered;
    public bool IsOpen
    {
        get { return isOpen; }
        set
        {
            isOpen = value;
            dropdown.SetActive(isOpen);
        }
    }

    private new void Awake()
    {
        base.Awake();

        // Needs to be here because Selectables (and thus also Buttons) use a version of ExecuteInEditMode
        // which causes Awake to be called after exiting in editor
        if (!Application.isPlaying) return;

        if (template == null)
            template = dropdown.transform.GetChild(0).gameObject;
        template.SetActive(false);
        if (templateDivider == null)
            templateDivider = dropdown.transform.GetChild(1).gameObject;
        templateDivider.SetActive(false);

        onClick.AddListener(() =>
        {
            IsOpen = !IsOpen;
        });

        foreach (Transform child in dropdown.transform)
        {
            if (!child.gameObject.activeSelf) continue;
            Destroy(child.gameObject);
        }
        foreach (var entry in entries)
        {
            GameObject go = Instantiate(template, dropdown.transform);
            var images = go.GetComponentsInChildren<Image>();
            var texts = go.GetComponentsInChildren<TMP_Text>();

            go.GetComponent<Button>().onClick.AddListener(() => entry.onSelect.Invoke());

            go.name = entry.value;
            texts[0].text = entry.value;
            texts[1].text = entry.info;
            if (entry.icon == null) images[1].color = Color.clear;
            else images[1].sprite = entry.icon;
            images[2].enabled = false;

            go.SetActive(true);

            if (entry.divideAfter)
            {
                Instantiate(templateDivider, dropdown.transform).SetActive(true);
            }
        }
    }
    private new void OnDisable()
    {
        base.OnDisable();
        IsOpen = false;
    }

    public void Show()
    {
        if (!IsOpen) IsOpen = true;
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
        isHovered = true;
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);

        isHovered = false;
        if (IsOpen)
        {
            IsOpen = false;
            StartCoroutine(OpenOther());
        }
    }

    private IEnumerator OpenOther()
    {
        for (int i = 0; i < Application.targetFrameRate / 5; i++)
        {
            yield return null;
            foreach (Transform sibling in transform.parent)
            {
                if (sibling.TryGetComponent<ContextDropdown>(out var menu))
                {
                    if (menu.isHovered)
                    {
                        menu.IsOpen = true;
                        break;
                    }
                }
            }
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        dropdown.SetActive(isOpen);
    }
#endif
}

[Serializable]
public class MenubarListEntry
{
    public Sprite icon;
    public string value;
    public string info;
    public UnityEvent onSelect;
    public bool divideAfter;
}

#if UNITY_EDITOR
[CustomEditor(typeof(ContextDropdown))]
public class MenubarDropdownTestEditor : UnityEditor.UI.ButtonEditor
{
    public override void OnInspectorGUI()
    {
        ContextDropdown component = (ContextDropdown)target;
        base.OnInspectorGUI();

        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("isOpen"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("entries"));
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
