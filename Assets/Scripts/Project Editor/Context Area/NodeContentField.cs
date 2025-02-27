using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.LookDev;
using UnityEngine.UI;

[RequireComponent (typeof(TMP_Dropdown))]
public class NodeContentField : ConfigActor
{
    [SerializeField] private ConfigField<int> configField;
    [SerializeField] private TMP_InputField inputField;
    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(i =>
        {
            Context.editor.ExecuteCommand(new ChangeValueCommand<int>(-i - 1, configField, err => { }));
        });

        inputField.gameObject.SetActive(false);
        inputField.onEndEdit.AddListener(AddEntry);
        //inputField.onSubmit.AddListener(AddEntry);
    }

    protected override void OnContextChange()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        Context.OnCategoryNameChange.AddListener(() =>
        {
            UpdateList();
        });
        configField.onFieldChange.AddListener(i =>
        {
            if (i < 0)
            {
                i = -i - 1;
                dropdown.SetValueWithoutNotify(i);
            }
            else
            {
                // TODO: idk
            }
        });
    }

    private void UpdateList()
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(Context.Config.categoryNames.Select(cName => new TMP_Dropdown.OptionData() { text = cName }).ToList());
        if (dropdown.IsExpanded)
        {
            dropdown.Hide();
            dropdown.Show();
        }
    }

    public void DestroyEntry(int index)
    {
        var deleteNodeContent = new CDNodeContentIndexCommand(index);
        if (dropdown.value >= index)
        {
            MultiCommand multi = new(
                "Delete CategoryName",
                false,
                new ChangeValueCommand<int>(-index, configField, err => { }),
                deleteNodeContent);
            dropdown.SetValueWithoutNotify(index - 1);
            Context.editor.ExecuteCommand(multi);

            return;
        }
        Context.editor.ExecuteCommand(deleteNodeContent);
    }

    public void AddEntry()
    {
        string name = null;
        ValidateName(ref name);

        inputField.SetTextWithoutNotify(name);
        inputField.gameObject.SetActive(true);
        inputField.Select();
    }
    private void AddEntry(string name)
    {
        ValidateName(ref name);

        MultiCommand multi = new(
            "Add CategoryName",
            false,
            new CDNodeContentIndexCommand(name),
            new ChangeValueCommand<int>(-Context.Config.categoryNames.Count - 1, configField, err => { }));

        Context.editor.ExecuteCommand(multi);
        inputField.gameObject.SetActive(false);
    }

    public void RenameEntry(int index, Transform entryTransform)
    {
        TMP_InputField localInputField = entryTransform.GetComponentInChildren<TMP_InputField>(true);
        UnityAction<string> onEndEdit = null;

        localInputField.gameObject.SetActive(true);
        localInputField.text = dropdown.options[index].text;
        localInputField.Select();

        onEndEdit = name =>
        {
            localInputField.onEndEdit.RemoveListener(onEndEdit);
            if (string.IsNullOrEmpty(name)) return;

            Context.editor.ExecuteCommand(new RenameCategoryNameCommand(index, name));
            localInputField.gameObject.SetActive(false);
            UpdateList();
        };
        localInputField.onEndEdit.AddListener(onEndEdit);
    }

    public void MoveEntry(int from, int to, bool rebase)
    {
        throw new NotImplementedException();
    }

    private void ValidateName(ref string name)
    {
        if (!string.IsNullOrEmpty(name)) return;

        string templateName = "New Category";
        int occurrences = Context.Config.categoryNames.FindAll(cName => cName.Equals(templateName)).Count;
        name = templateName + (occurrences < 1 ? "" : " " + (occurrences + 1).ToString());
    }
}
