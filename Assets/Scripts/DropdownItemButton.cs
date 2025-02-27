using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class DropdownItemButton : MonoBehaviour
{
    [SerializeField] private Transform entryTransform;
    [SerializeField] private UnityEvent<int> OnClickIndex;
    [SerializeField] private UnityEvent<int, Transform> OnClickIndexTransform;

    private void Start()
    {
        if (entryTransform == null) entryTransform = transform.parent;

        Button button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            int siblingIndex = entryTransform.GetSiblingIndex() - 1;
            OnClickIndex.Invoke(siblingIndex);
            OnClickIndexTransform.Invoke(siblingIndex, entryTransform);
        });
        //NodeContentField ncField = GetComponentInParent<NodeContentField>();
        //button.onClick.AddListener(() => ncField.DestroyEntry(entryTransform.GetSiblingIndex() - 1));
    }
}
