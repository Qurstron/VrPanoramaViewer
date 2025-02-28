using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Places the UI element around the Cursor without obsructiong it or leving the screen.
/// Assumes an Anchor of (0, 1) to work.
/// Note: If the the combined object size and the screenPadding is bigger than the screen, its behavior is undefined
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MouseFitter : MonoBehaviour
{
    [Tooltip("The Acutal offset may differ in favor of fitting the object in the screen")]
    [SerializeField] private Vector2 preferedOffset = Vector2.zero;
    [Tooltip("Describes the minimum distance between the object and the screen")]
    [SerializeField] private Vector2 screenPadding = Vector2.zero;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }
    private void OnEnable()
    {
        float scale = transform.root.localScale.x;
        Vector2 targetPos = (Vector2)Input.mousePosition + preferedOffset;
        // We flip the y-coordinate, because unity uses a origin in the bottom left corner
        // but adding the rect size in this way only works by converting it
        targetPos.y = Screen.height - targetPos.y;
        targetPos.x = Mathf.Max(screenPadding.x, targetPos.x);
        targetPos.y = Mathf.Max(screenPadding.y, targetPos.y);

        Vector2 farSidePos = targetPos + (rectTransform.rect.size + screenPadding) * scale;
        targetPos.x -= Mathf.Max(0, farSidePos.x - Screen.width);
        targetPos.y -= Mathf.Max(0, farSidePos.y - Screen.height);

        targetPos.y = Screen.height - targetPos.y;
        transform.position = targetPos;
    }
}
