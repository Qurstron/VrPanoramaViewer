using UnityEngine;
using UnityEngine.Events;

// Canvas is not strictly needed, but a view is a nice point for a nested canvas optimization
// https://learn.unity.com/tutorial/nested-canvas-optimization-2018-4#5e5e841fedbc2a0f0e6bb482
[RequireComponent(typeof(Canvas))]
public class View : MonoBehaviour
{
    [SerializeField] private bool isIntermediate = false; // inidcates if the view should be recorded in the menu stack
    [SerializeField] private bool isTransparent = false;
    [SerializeField] private bool overridingRunBackground = false;
    protected ViewController viewController;

    public Canvas Canvas { get; private set; }
    public bool IsIntermediate { get { return isIntermediate; } }
    public bool IsTransparent { get { return isTransparent; } }
    public bool OverridingRunBackground { get { return overridingRunBackground; } }
    public bool IsTop { get { return viewController.CurrentView == this; } }
    public bool IsInViewStack { get { return viewController.Contains(this); } }

    public UnityEvent OnShow = new();
    public UnityEvent OnHide = new();

    private void Awake()
    {
        viewController = transform.root.GetComponent<ViewController>();
        Canvas = GetComponent<Canvas>();
    }

    public void Display()
    {
        //if (viewController == null) viewController = transform.root.GetComponent<ViewController>();
        viewController.DisplayView(this);
    }
    public void UnDisplay()
    {
        if (IsTop) viewController.Back();
    }
    public void ForceUnDisplay()
    {
        viewController.Back();
    }
}
