using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

[Serializable]
public class Category : UnityEngine.UI.Button
{
    [SerializeField] private LayoutElement layoutElement;
    public GameObject content;
    public Image arrow;
    public bool closeRecursively = false;
    private bool isExpanded = false;
    public bool IsExpanded {
        get { return isExpanded; }
        set
        {
            // Dropdown animation was canceled do to technical difficulties
            // But the somewhat working code is commented below for anyone to try to get it working
            SetExpandedNoAnim(value);

            //isExpanded = value;
            //if (isExpanded)
            //{
            //    content.SetActive(true);
            //    UpdateChilderen();
            //}
            //RectTransform rectTransform = content.GetComponent<RectTransform>();

            //GameObject rootCat = gameObject;
            //List<RectTransform> rects = new();
            //while (rootCat.transform.parent.gameObject.GetComponentInParent<Category>() != null)
            //{
            //    rootCat = rootCat.transform.parent.gameObject;
            //    rects.Add(rootCat.GetComponent<RectTransform>());
            //}
            //RectTransform rootRect = rootCat.GetComponent<RectTransform>();

            //float low = 1;
            //float high = 0;
            //var lastSequence = DOTween.Sequence();
            //if (isExpanded)
            //{
            //    low = 0;
            //    high = 1;
            //}

            //lastSequence.onComplete = () =>
            //{
            //    if (!IsExpanded)
            //    {
            //        if (closeRecursively) closeRec();
            //        content.SetActive(false);
            //    }
            //};

            //if (arrow != null)
            //{
            //    lastSequence.Append(arrow.transform.DOLocalRotate(new Vector3(0, 0, -90 * high), .1f));
            //}
            //lastSequence.Join(DOVirtual.Float(low, high, .1f, t =>
            //{
            //    rectTransform.GetComponent<LayoutElement>().minHeight = LayoutUtility.GetPreferredSize(rectTransform, 1) * t;
            //}));
        }
    }
    public Transform ContentTarget { get { return content.transform; } }

    protected override void Start()
    {
        base.Start();

        //content.SetActive(false);
        onClick.AddListener(() =>
        {
            IsExpanded = !IsExpanded;
        });
    }
    protected void closeRec()
    {
        bool skipFirst = true;
        foreach (var cat in GetComponentsInChildren<Category>())
        {
            if (skipFirst)
            {
                skipFirst = false;
                continue;
            }

            cat.closeRec();
            cat.arrow.transform.localRotation = Quaternion.identity;
            cat.isExpanded = false;
            cat.content.SetActive(false);
        }
    }

    /// <summary>
    /// Toggels the expanded state without animations
    /// </summary>
    public void ToggleNoAnim()
    {
        isExpanded = !isExpanded;
        content.SetActive(IsExpanded);

        if (IsExpanded) arrow.transform.localRotation = Quaternion.Euler(0f, 0f, -90);
        else arrow.transform.localRotation = Quaternion.identity;

        UpdateContentSize();
    }
    /// <summary>
    /// Sets the expanded state without animations 
    /// </summary>
    public void SetExpandedNoAnim(bool isExpanded)
    {
        if (this.isExpanded == isExpanded) return;
        ToggleNoAnim();
    }
    public void UpdateContentSize()
    {
        // we need 2 ForceRebuildLayoutImmediate, because GetPreferredSize has problems calculating the correct size
        // when called on the same frame the gameobject is instantiated/Enabled

        RectTransform rectTransform = content.GetComponent<RectTransform>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, LayoutUtility.GetPreferredSize(rectTransform, 1));
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        
    }
    public void UpdateDelayed()
    {
        StartCoroutine(UpdateLayoutGroup());
    }

    private IEnumerator UpdateLayoutGroup()
    {
        yield return new WaitForEndOfFrame();
        UpdateContentSize();
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateDelayed();
    }

    private void UpdateChilderen()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Category cat))
                cat.UpdateChilderen();
        }
        UpdateContentSize();
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Category))]
public class CategoryEditor : UnityEditor.UI.ButtonEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        serializedObject.Update();

        Category targetMenuButton = (Category)target;
        targetMenuButton.content = EditorGUILayout.ObjectField("content", targetMenuButton.content, typeof(GameObject), true) as GameObject;
        targetMenuButton.arrow = EditorGUILayout.ObjectField("arrow", targetMenuButton.arrow, typeof(Image), true) as Image;
        targetMenuButton.closeRecursively = EditorGUILayout.Toggle("close Recursively", targetMenuButton.closeRecursively);
        if (GUILayout.Button("toggle cat"))
        {
            targetMenuButton.ToggleNoAnim();
        }
        //targetMenuButton.IsExpanded = EditorGUILayout.Toggle("Is Expanded", targetMenuButton.IsExpanded);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
