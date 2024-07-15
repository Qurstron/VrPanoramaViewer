using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class VisibilityButton : MonoBehaviour
{
    public Image visible;
    public Image hidden;
    public CreateTestGraph graph;
    public SpriteRenderer spriteRenderer;
    public float hidePopupSpeed = 1f;
    public float hidePopupDelay = 1f;

    private Button button;

    // Start is called before the first frame update
    void Start()
    {
        button = GetComponent<Button>();
        Color originalColor = spriteRenderer.color;

        hidden.enabled = false;
        spriteRenderer.enabled = false;

        button.onClick.AddListener(() =>
        {
            bool isVisible = !visible.enabled;
            visible.enabled = isVisible;
            hidden.enabled = !isVisible;

            var sequence = graph.SetIsGraphEnabled(isVisible);
            if (sequence != null && !isVisible)
            {
                spriteRenderer.enabled = true;
                sequence.Append(DOVirtual.Color(new Color(1, 1, 1, 0), originalColor, hidePopupSpeed, (value) =>
                {
                    spriteRenderer.color = value;
                }).SetDelay(hidePopupDelay));
            }
            else
            {
                spriteRenderer.color = new Color(1, 1, 1, 0);
                spriteRenderer.enabled = false;
            }

            return;
        });
    }
}
