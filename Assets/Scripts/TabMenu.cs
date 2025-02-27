using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TabMenu : MonoBehaviour
{
    [SerializeField] private GameObject tabPrefab;
    [SerializeField] private Transform tabTarget;
    [Tooltip("Sets the PreferedHeight of an LayoutElement to the current tab, Optional")]
    [SerializeField] private LayoutElement refitTarget;
    [Tooltip("If preferedTabStartIndex is bigger than the amount of tabs, default to 0")]
    [SerializeField] private int preferedTabStartIndex = 0;
    private Tab[] tabs;
    private Tab selectedTab = null;
    private Button selectedButton = null;

    private void Start()
    {
        foreach (GameObject child in tabTarget)
        {
            Destroy(child);
        }

        tabs = GetComponentsInChildren<Tab>();
        if (tabs.Length <= 0)
        {
            Debug.LogWarning("No Tabs in TabMenu, this is probably unintended");
            return;
        }
        if (tabs.Length <= preferedTabStartIndex) preferedTabStartIndex = 0;

        for (int i = 0; i < tabs.Length; i++)
        {
            Tab tab = tabs[i];
            GameObject tabButtonObject = Instantiate(tabPrefab, tabTarget);
            tab.button = tabButtonObject.GetComponentInChildren<Button>();

            tab.button.onClick.AddListener(() => SelectTab(tab));
            tabButtonObject.GetComponentInChildren<TMP_Text>().text = tab.name;

            if (i == preferedTabStartIndex) SelectTab(tab);
            else tab.gameObject.SetActive(false);
        }
    }
    //private void SelectTab(Tab tab, Button tabButton)
    //{
    //    if (selectedButton != null)
    //    {
    //        selectedTab.gameObject.SetActive(false);
    //        selectedButton.interactable = true;
    //    }

    //    tabButton.interactable = false;
    //    tab.gameObject.SetActive(true);
    //    selectedTab = tab;
    //    selectedButton = tabButton;

    //    if (refitTarget != null)
    //    {
    //        refitTarget.preferredHeight = tab.gameObject.GetComponent<RectTransform>().rect.height;
    //    }
    //}
    public void SelectTab(Tab tab)
    {
        if (selectedButton != null)
        {
            selectedTab.gameObject.SetActive(false);
            selectedButton.interactable = true;
        }

        if (tab.button != null)
            tab.button.interactable = false;
        tab.gameObject.SetActive(true);
        selectedTab = tab;
        selectedButton = tab.button;

        if (refitTarget != null)
        {
            refitTarget.preferredHeight = tab.gameObject.GetComponent<RectTransform>().rect.height;
        }
    }

    /// <summary>
    /// Switches the currenty selected tab to the one containing the child.
    /// This has no effect when provided a child that is not a child of any tab.
    /// </summary>
    public void SelectTabByChild(Transform child)
    {
        Tab parentTab = null;

        for (int i = 0; i < tabs.Length; i++)
        {
            Tab tab = tabs[i];
            if (child.IsChildOf(tab.transform))
            {
                parentTab = tab;
                break;
            }
        }

        if (parentTab != null) SelectTab(parentTab);
    }

//#if UNITY_EDITOR
//    private void OnValidate()
//    {

//    }
//#endif
}
