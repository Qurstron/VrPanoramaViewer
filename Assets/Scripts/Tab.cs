using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class Tab : MonoBehaviour
{
    public Button button;
    [SerializeField] private TabMenu menu;

    private void Start()
    {
        button.onClick.AddListener(Select);
    }

    public void Select()
    {
        if (menu == null)
            menu = GetComponentInParent<TabMenu>();
        menu.SelectTab(this);
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(Tab)), CanEditMultipleObjects]
public class TabInspector : Editor
{
    public override void OnInspectorGUI()
    {
        Tab tab = (Tab)target;

        DrawDefaultInspector();
        if (GUILayout.Button("View this tab"))
        {
            // TODO: Fix
            tab.Select();
        }
    }
}
#endif
