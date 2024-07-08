using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Rendering;
using static JSONClasses;

[RequireComponent(typeof(XRBaseInteractable))]
public class NodeProperties : MonoBehaviour
{
    //private MaterialPropertyBlock propertyBlock;
    public MaterialPropertyBlockHelper propertyHelper;
    private Color color = new();
    private string displayName = "";
    public Color Color
    {
        get { return color; }
        set
        {
            color = value;
            if (light != null)
            {
                Color linearColor = color.linear;
                Vector3 vecColor = new Vector3(linearColor.r, linearColor.g, linearColor.b);
                vecColor.Normalize();
                light.color = (Color)new Vector4(vecColor.x, vecColor.y, vecColor.z, 1);
            }

            // this works but not compatiable
            //if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
            //meshRenderer.GetPropertyBlock(propertyBlock, 0);
            //propertyBlock.SetColor(colorPropertyName, color * materialInstesity);
            //meshRenderer.SetPropertyBlock(propertyBlock);

            if (propertyHelper == null) return;
            MaterialPropertyBlock propertyBlock = propertyHelper.GetMaterialPropertyBlock();
            propertyBlock?.SetColor(colorPropertyName, color);
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

    [Header("Strings")]
    public TMP_Text displayText;

    [Header("GameObject specific")]
    public bool isForceDrop = false;
    public bool positionLockOverride = false;
    private bool isPositionLocked = false; // indicates if the node positon is driven by animation
    public bool IsPositionLocked
    {
        get { return isPositionLocked; }
        set
        {
            isPositionLocked = value;
            if (isPositionLocked)
            {
                force = Vector3.zero;
            }
        }
    }
    private Vector3 force;
    public Vector3 Force
    {
        get { return force; }
        set
        {
            if (positionLockOverride) return;
            if (isPositionLocked) return;
            force = value;
        }
    }
    public Node node;
    public Vector3 originalPos = Vector3.zero; // local space
    public Vector3 TheoreticalPosition
    {
        get
        {
            if (IsPositionLocked || positionLockOverride) return originalPos;
            return transform.localPosition;
        }
    }
    public XRBaseInteractable interactable;

    private void Start()
    {
        if (interactable == null) interactable = GetComponent<XRBaseInteractable>();
    }

    public override bool Equals(object obj)
    {
        return obj is NodeProperties properties &&
               base.Equals(obj) &&
               EqualityComparer<Node>.Default.Equals(node, properties.node);
    }
    public override int GetHashCode()
    {
        return HashCode.Combine(base.GetHashCode(), node);
    }
}
