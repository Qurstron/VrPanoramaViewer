using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sets the alphaHitTestMinimumThreshold of an Image.
/// Should probably be a setting in the Image it self, but here we are.
/// </summary>
[RequireComponent(typeof(Image))]
public class ImageHitAlpha : MonoBehaviour
{
    [SerializeField] private float minAlpha = .5f;

    void Start()
    {
        GetComponent<Image>().alphaHitTestMinimumThreshold = minAlpha;
    }
}
