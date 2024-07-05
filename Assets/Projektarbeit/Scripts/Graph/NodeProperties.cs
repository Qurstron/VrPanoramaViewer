using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Rendering;

public class NodeProperties : MonoBehaviour
{
    private MaterialPropertyBlock propertyBlock;
    private MaterialPropertyBlockHelper propertyHelper;
    private Color color = new();
    private string displayName = "";
    public Color Color
    {
        get { return color; }
        set
        {
            color = value;
            if (light != null) light.color = color;
            //color *= 2;

            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(propertyBlock, 0);
            propertyBlock.SetColor(colorPropertyName, color * materialInstesity);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }
    public string DisplayName
    {
        get { return displayName; }
        set
        {
            displayName = value;
            displayText.text = displayName;
        }
    }

    [Header("Color")]
    public Light light;
    //public Material material;
    public MeshRenderer meshRenderer;
    public string colorPropertyName;
    public float materialInstesity = 3f;
    [Header("Strings")]
    public TMP_Text displayText;
}
