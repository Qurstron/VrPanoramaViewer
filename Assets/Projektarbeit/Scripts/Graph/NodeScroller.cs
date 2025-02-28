using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NodeScroller : MonoBehaviour
{
    public Material material;
    public float factor = 1.0f;
    public float higherFactor = 1.0f;

    // Update is called once per frame
    void Update()
    {
        float offset = Time.realtimeSinceStartup * factor;
        float higherOffset = offset * higherFactor;

        material.SetVector("_Offset", new(offset, offset));
        material.SetVector("_HigherOffset", new(higherOffset, higherOffset));
    }
}
