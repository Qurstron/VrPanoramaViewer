using UnityEngine;

public class CursorHandler : MonoBehaviour
{
    [SerializeField] private Texture2D link;
    [SerializeField] private Vector2 linkHotspot = Vector2.zero;
    [SerializeField] private Texture2D bar;
    [SerializeField] private Vector2 barHotspot = Vector2.zero;
    /// <summary>
    /// A Semaphore is needed because overlapping ui cursor elements could override eachother
    /// </summary>
    //private int semaphore = 0;

    public enum CursorType
    {
        Default,
        Link,
        Bar
    }

    public void SetCursor(CursorType type)
    {
        //semaphore++;
        switch (type)
        {
            case CursorType.Default:
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                break;
            case CursorType.Link:
                Cursor.SetCursor(link, linkHotspot, CursorMode.Auto);
                break;
            case CursorType.Bar:
                Cursor.SetCursor(bar, barHotspot, CursorMode.Auto);
                break;

            default:
                throw new System.Exception("Invalid CursorType");
        }
    }
    public void Unset()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }
}
