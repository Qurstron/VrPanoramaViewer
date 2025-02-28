using JSONClasses;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class NodeContentParentInputField : ConfigActor
{
    [SerializeField] private ConfigField<List<int>> configField;
    [SerializeField] private ConfigField<int> currentNCField;
    [SerializeField] private GraphNavigator graphNavigator;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private RectTransform availableNCTarget;
    [SerializeField] private RectTransform actualNCTarget;
    [SerializeField] private GameObject addConfigNCButton;
    [SerializeField] private Transform objectParent;
    private RectTransform scrollViewAvailable;
    private RectTransform scrollViewActual;
    private GameObject emptySpace;
    private float entryHeight;
    private int entryIndex;

    protected override void OnContextChange()
    {
        Context.OnNodeContentChange.AddListener(() => OnParentChange(Context.currentNodeContent.categoryParentIndices));
    }

    public void CreateConfigNC()
    {
        NodeContent nc = new()
        {
            name = "New NC"
        };
        Context.editor.ExecuteCommand(new CDConfigNCCommand(nc, false));
        Context.editor.ExecuteCommand(new ChangeValueCommand<int>(Context.Config.categoryParents.Count - 1, currentNCField, err => { }));
    }

    private void Start()
    {
        entryHeight = (entryPrefab.transform as RectTransform).rect.height;
        emptySpace = new GameObject("Empty Space", typeof(RectTransform));
        emptySpace.SetActive(false);
        (emptySpace.transform as RectTransform).sizeDelta = new(1, entryHeight);
        emptySpace.transform.SetParent(actualNCTarget, false);

        scrollViewAvailable = availableNCTarget.GetComponentInParent<ScrollRect>().transform as RectTransform;
        scrollViewActual = actualNCTarget.GetComponentInParent<ScrollRect>().transform as RectTransform;

        addConfigNCButton.GetComponentInChildren<Button>().onClick.AddListener(CreateConfigNC);
    }

    private void OnParentChange(List<int> parents)
    {
        foreach (Transform child in availableNCTarget)
        {
            if (child == addConfigNCButton.transform) continue;
            Destroy(child.gameObject);
        }
        foreach (Transform child in actualNCTarget)
        {
            if (child == emptySpace.transform) continue;
            Destroy(child.gameObject);
        }
        if (Context.Config.categoryParents == null) return;

        // Potential Parents
        int i = 0;
        foreach (NodeContent nc in Context.Config.categoryParents)
        {
            NCParentEntry entry = CreateEntry(nc, availableNCTarget, i);
            i++;
        }
        if (Context.currentNode != null)
        {
            i = 0;
            foreach (Node neighbor in Context.currentNode.NodeNeighbors)
            {
                if (neighbor.content.Count <= Context.currentNodeContent.indexInNode)
                    continue;

                CreateEntry(neighbor.content[Context.currentNodeContent.indexInNode], availableNCTarget, -(i + 1));
                i++;
            }
        }
        addConfigNCButton.transform.SetAsLastSibling();

        // Actual Parents
        List<Node> nodeContentsNode = new();
        i = 0;
        foreach (NodeContent nc in Context.currentNodeContent.Parents)
        {
            NCParentEntry entry = CreateEntry(nc, actualNCTarget, Context.currentNodeContent.categoryParentIndices[i]);
            entry.isActualParent = true;
            entry.index = i;

            if (nc.node != null) nodeContentsNode.Add(nc.node);

            i++;
        }

        graphNavigator.HighlightEdgesFromNode(Context.currentNode, nodeContentsNode.ToArray());
    }
    private NCParentEntry CreateEntry(NodeContent nc, Transform target, int parentIndex)
    {
        GameObject entry = Instantiate(entryPrefab, target);
        DragableUIElement dragable = entry.GetComponent<DragableUIElement>();
        NCParentEntry ncParentEntry = entry.GetComponent<NCParentEntry>();
        DoubleClick doubleClick = entry.GetComponent<DoubleClick>();
        Button addButton = entry.GetComponentsInChildren<Button>(true)[1];
        var texts = entry.GetComponentsInChildren<TMP_Text>();
        bool isConfigNC = nc.node == null;
        string name = isConfigNC ? nc.name : nc.node.name;

        ncParentEntry.parentIndex = parentIndex;

        dragable.parent = objectParent;
        dragable.onDrag.AddListener(OnEntryDrag);
        dragable.onFinishedDrag.AddListener(OnEndEntryDrag);

        if (string.IsNullOrEmpty(name)) name = "NodeContent";
        entry.name = name;
        texts[0].text = name;
        texts[1].text = $"({(isConfigNC ? "Config" : "Node")})";

        if (isConfigNC)
        {
            doubleClick.OnDoubleClick.AddListener(_ =>
            {
                Context.editor.ExecuteCommand(new ChangeValueCommand<int>(ncParentEntry.parentIndex, currentNCField, err => { }));
            });
            ncParentEntry.OnHoverChange.AddListener(isHovered =>
            {
                addButton.gameObject.SetActive(isHovered);
            });
            addButton.onClick.AddListener(() =>
            {
                Rename(nc, entry.transform);
            });
            addButton.gameObject.SetActive(false);
        }
        else
        {
            doubleClick.OnDoubleClick.AddListener(_ =>
            {
                Context.editor.ExecuteCommand(new ChangeSelectedNode(nc.node));
            });
            Destroy(addButton.gameObject);
        }

        return ncParentEntry;
    }
    private void Rename(NodeContent nc, Transform entryTransform)
    {
        TMP_InputField localInputField = entryTransform.GetComponentInChildren<TMP_InputField>(true);
        UnityAction<string> onEndEdit = null;

        localInputField.gameObject.SetActive(true);
        localInputField.text = nc.name;
        localInputField.Select();

        onEndEdit = name =>
        {
            localInputField.onEndEdit.RemoveListener(onEndEdit);
            if (string.IsNullOrEmpty(name)) return;

            Context.editor.ExecuteCommand(new RenameObjectCommand(nc, name));
            localInputField.gameObject.SetActive(false);
            //UpdateList();
        };
        localInputField.onEndEdit.AddListener(onEndEdit);
    }

    private void OnEntryDrag(Vector2 pos, GameObject entry)
    {
        NCParentEntry ncParentEntry = entry.GetComponent<NCParentEntry>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollViewActual, Input.mousePosition, null, out Vector2 actualLocalPos);

        if (scrollViewActual.rect.Contains(actualLocalPos))
        {
            if (actualLocalPos.y > 0) entryIndex = 0;
            else entryIndex = Mathf.RoundToInt(-actualLocalPos.y / entryHeight);
            entryIndex = Math.Min(entryIndex, Context.currentNodeContent.categoryParentIndices.Count);
            emptySpace.transform.SetSiblingIndex(entryIndex);

            if (ncParentEntry.isActualParent && (ncParentEntry.index == entryIndex || ncParentEntry.index == entryIndex - 1))
                DeactivateEntryIndex();
            else if (!emptySpace.activeSelf)
                emptySpace.SetActive(true);
        }
        else
        {
            DeactivateEntryIndex();
        }
    }
    private void OnEndEntryDrag(Vector2 pos, GameObject entry)
    {
        // place new entry in the location (index) if it was droped over actualNCTarget
        NCParentEntry ncEntry = entry.GetComponent<NCParentEntry>();
        List<int> parents = new(configField.GetField(Context));
        emptySpace.SetActive(false);

        if (ncEntry.isActualParent)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(scrollViewAvailable, Input.mousePosition, null, out Vector2 potentialLocalPos);
            if (scrollViewAvailable.rect.Contains(potentialLocalPos))
            {
                // remove ncEntry.index
                parents.RemoveAt(ncEntry.index);
                Context.editor.ExecuteCommand(new ChangeValueCommand<List<int>>(parents, configField, err => { }));
            }
            else if (entryIndex >= 0)
            {
                // move from ncEntry.index to entryIndex
                parents.RemoveAt(ncEntry.index);
                if (entryIndex >= parents.Count) parents.Add(ncEntry.parentIndex);
                else parents.Insert(entryIndex > ncEntry.index ? entryIndex + 1 : entryIndex, ncEntry.parentIndex);
                Context.editor.ExecuteCommand(new ChangeValueCommand<List<int>>(parents, configField, err => { }));
            }
        }
        else if (entryIndex >= 0)
        {
            // add at entryIndex
            if (entryIndex >= parents.Count)
                parents.Add(ncEntry.parentIndex);
            else
                parents.Insert(entryIndex, ncEntry.parentIndex);

            Context.editor.ExecuteCommand(new ChangeValueCommand<List<int>>(parents, configField, err => { }, true));
        }
    }
    private void DeactivateEntryIndex()
    {
        emptySpace.SetActive(false);
        entryIndex = -1;
    }
}
