using JSONClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldOutline : ConfigActor
{
    [SerializeField] private Transform entryAngleTarget;
    [SerializeField] private Transform entryObjectTarget;
    [SerializeField] private GameObject categoryPrefab;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private ExcludeField excludeField;
    [SerializeField] private Transform objectParent;

    private readonly Dictionary<ObjectSelectable, GameObject> objEntries = new();
    private readonly Dictionary<AngleSelectable, GameObject> angleEntries = new();
    private readonly Dictionary<Type, GameObject> displayedTypes = new();

    protected override void OnContextChange()
    {
        // Change blue tint in entries
        Context.OnSelectionChange.AddListener(() =>
        {
            ClearSelected();

            foreach (AnglePoint anglePoint in Context.selectedAngles)
            {
                if (angleEntries.TryGetValue(anglePoint.relatedComponent, out GameObject go))
                {
                    go.GetComponentInChildren<Image>().enabled = true;
                }
            }
            foreach (ObjectSelectable objectSelectable in Context.selectedObjects)
            {
                if (objEntries.TryGetValue(objectSelectable, out GameObject go))
                {
                    go.GetComponentInChildren<Image>().enabled = true;
                }
                //objectSelectable.IsSelected = true;
            }
        });
        // Change title
        Context.OnNodeContentChange.AddListener(() =>
        {
            string nodeName = Context.currentNode != null ? Context.currentNode.name : "Config";
            GetComponentInChildren<TMP_Text>().text = $"{nodeName} - {Context.currentNodeContent.name}";
        });
    }

    /// <summary>
    /// Fills the World Outline display
    /// </summary>
    /// <remarks>Gets called by PanoramaSphereController on update level OnNodeContentChange</remarks>
    public void Setup(List<WorldSelectable> selectables)
    {
        angleEntries.Clear();
        displayedTypes.Clear();
        foreach (Transform child in entryAngleTarget)
        {
            Destroy(child.gameObject);
        }

        foreach (var selectable in selectables)
        {
            GameObject categoryObj;

            if (displayedTypes.TryGetValue(selectable.GetType(), out GameObject cat)) categoryObj = cat;
            else
            {
                categoryObj = Instantiate(categoryPrefab, entryAngleTarget);
                categoryObj.GetComponentInChildren<TMP_Text>().text = selectable.GetType().Name;
                displayedTypes.Add(selectable.GetType(), categoryObj);
            }

            CreateEntry(selectable, categoryObj.GetComponent<Category>().ContentTarget);
        }

        ClearSelected();
        foreach (GameObject categoryObj in displayedTypes.Values)
        {
            categoryObj.GetComponent<Category>().SetExpandedNoAnim(true);
        }

        entryAngleTarget.GetComponentInParent<Category>()?.SetExpandedNoAnim(true);
        entryObjectTarget.GetComponentInParent<Category>()?.SetExpandedNoAnim(true);
    }
    public void Setup(List<SceneRoot> sceneRoots)
    {
        objEntries.Clear();
        foreach (Transform child in entryObjectTarget)
        {
            Destroy(child.gameObject);
        }

        foreach (SceneRoot root in sceneRoots)
        {
            Category category = Instantiate(categoryPrefab, entryObjectTarget).GetComponent<Category>();
            category.GetComponentInChildren<TMP_Text>().text = root.gameObject.name;
            category.gameObject.name = root.gameObject.name;
            category.SetExpandedNoAnim(true);

            foreach (Transform child in root.transform)
            {
                CreateObjectEntry(child, category.ContentTarget);
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="transform"></param>
    /// <param name="target"></param>
    /// <returns>True if subtree contains a selectable</returns>
    private void CreateObjectEntry(Transform transform, Transform target)
    {
        if (transform.childCount <= 0 && !transform.TryGetComponent<ObjectSelectableContainer>(out _)) return;

        Button button = null;
        GameObject go = null;
        if (transform.childCount <= 0 || transform.GetComponentsInChildren<ObjectSelectableContainer>().Length < 2)
        {
            go = Instantiate(entryPrefab, target);
            button = go.GetComponent<Button>();
            SetEntryInfo(go, transform.gameObject.name, null);
            Destroy(go.GetComponentInChildren<DragableUIElement>());
            var selectable = QUtils.GetOrAddComponent<ObjectSelectableContainer>(transform).selectable;
            if (selectable != null)
                objEntries.Add(QUtils.GetOrAddComponent<ObjectSelectableContainer>(transform).selectable, go);
        }
        else
        {
            go = Instantiate(categoryPrefab, target);
            button = go.GetComponent<Button>();
            Category category = go.GetComponent<Category>();
            foreach (Transform child in transform)
            {
                CreateObjectEntry(child, category.ContentTarget);
            }

            if (transform.childCount == 1)
                category.SetExpandedNoAnim(true);
        }

        button.GetComponentInChildren<TMP_Text>().text = transform.gameObject.name;
        button.gameObject.name = transform.gameObject.name;

        button.onClick.AddListener(() =>
        {
            if (!transform.TryGetComponent<ObjectSelectableContainer>(out var selectable)) return;
            Context.editor.ExecuteCommand(new SelectCommand(new List<ObjectSelectable>() { selectable.selectable }, !Input.GetKey(KeyCode.LeftControl) || selectable.selectable.IsSelected));
        });
    }
    /// <summary>
    /// Creates a specific entry in a parent category
    /// </summary>
    /// <param name="parent">Parent category</param>
    public void CreateEntry(AngleSelectable selectable, Transform parent)
    {
        GameObject entry = Instantiate(entryPrefab, parent);
        DragableUIElement dragable = entry.GetComponent<DragableUIElement>();

        SetEntryInfo(entry, selectable.Name, selectable.GetOrigin(), selectable.GetPoints().Count);
        dragable.parent = objectParent;
        dragable.onBeginDrag.AddListener(excludeField.DragableBegin);
        dragable.onFinishedDrag.AddListener(excludeField.DragableFinish);
        // This is a bit hacky.
        // It would be nicer if the AngleSelectable could determin that it self
        // when that part is more developed
        QUtils.GetOrAddComponent<MergeSubjectContainer>(entry).mergeSubject = selectable.GetPoints()[0].relatedComponent.Subject;

        entry.GetComponent<Button>().onClick.AddListener(() =>
        {
            Context.editor.ExecuteCommand(new SelectCommand(selectable.GetPoints(), !Input.GetKey(KeyCode.LeftControl)));
        });
        entry.GetComponent<DoubleClick>().OnDoubleClick.AddListener(pos =>
        {
            Context.editor.FocusSelection();
        });

        angleEntries.Add(selectable, entry);
    }
    /// <summary>
    /// Clears the blue tint in all entries
    /// </summary>
    public void ClearSelected()
    {
        foreach (GameObject go in angleEntries.Values)
        {
            go.GetComponentInChildren<Image>().enabled = false;
        }
        foreach (GameObject go in objEntries.Values)
        {
            go.GetComponentInChildren<Image>().enabled = false;
        }
    }

    /// <summary>
    /// Set the text on the entryPrefab
    /// </summary>
    /// <param name="entry">Instantiated entryPrefab</param>
    /// <remarks>Also visually deselects the entry</remarks>
    private void SetEntryInfo(GameObject entry, string name, NodeContent ncOrigin, int pointCount = 0)
    {
        var texts = entry.GetComponentsInChildren<TMP_Text>();
        bool isConfigContent = ncOrigin == null || ncOrigin.node == null;
        string ncName = "";

        entry.GetComponentInChildren<Image>().enabled = false;
        texts[0].text = name;
        texts[1].text = pointCount > 0 ? $"({pointCount})" : "";

        if (!(Context.currentNodeContent.Equals(ncOrigin) || ncOrigin == null))
        {
            ncName += $"({(isConfigContent ? "Config" : "Node")} ";
            ncName += $"- {(isConfigContent ? ncOrigin.name : ncOrigin.node.name)})";
        }
        texts[2].text = ncName;
    }
}
