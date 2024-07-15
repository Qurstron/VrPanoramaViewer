using DG.Tweening;
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
    [Header("Color")]
    public Light light;
    public float defaultIntesity = 1f;
    public MeshRenderer meshRenderer;
    public string colorPropertyName;

    [Header("Text Components")]
    public TMP_Text titleText;
    public TMP_Text outerTitleText;
    public TMP_Text descriptionText;

    [Header("Dimensions")]
    public Transform scaleTarget;
    public Vector3 collapsedScale = Vector3.one;
    public Vector3 expandedScale = Vector3.one;
    public float timeToScale = 1f;
    public Ease ease = Ease.Linear;

    [Header("GameObject specific")]
    public MaterialPropertyBlockHelper propertyHelper;
    public bool isForceDrop = false;
    public bool positionLockOverride = false;
    public Vector3 originalPos = Vector3.zero; // local space
    public XRBaseInteractable interactable;
    public Node node;

    private Color color = new();
    private string displayName = "";
    private bool isPositionLocked = false; // indicates if the node positon is driven by animation
    private Vector3 force;
    private bool isExpanded = false;
    private Typewriter outerTitleTypewriter;
    private Typewriter descriptionTypewriter;
    private Sequence expandSequence;

    #region Properties
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
            if (propertyHelper != null)
            {
                MaterialPropertyBlock propertyBlock = propertyHelper.GetMaterialPropertyBlock();
                propertyBlock?.SetColor(colorPropertyName, color);
            }
        }
    }
    public string DisplayName
    {
        get { return displayName; }
        set
        {
            displayName = value;
            titleText.text = displayName;
            if (outerTitleTypewriter == null) outerTitleTypewriter = outerTitleText.transform.GetComponent<Typewriter>();
            outerTitleTypewriter.text = displayName;
            outerTitleTypewriter.ResetCursor();
            //displayText.text = displayName;
        }
    }
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
    public Vector3 TheoreticalPosition
    {
        get
        {
            if (IsPositionLocked || positionLockOverride) return originalPos;
            return transform.localPosition;
        }
    }
    public bool IsExpanded
    {
        get { return isExpanded; }
        set
        {
            isExpanded = value;
            expandSequence.Kill();

            if (propertyHelper != null)
            {
                MaterialPropertyBlock propertyBlock = propertyHelper.GetMaterialPropertyBlock();
                propertyBlock?.SetInt("_IsExpanded", isExpanded ? 1 : 0);
            }

            outerTitleTypewriter.isUntyping = isExpanded;
            outerTitleTypewriter.ResetCursor();

            positionLockOverride = isExpanded;
            if (isExpanded)
            {
                descriptionText.enabled = false;
                outerTitleTypewriter.UntypeText();
                return;
            }

            titleText.enabled = false;
            expandSequence = DOTween.Sequence();
            expandSequence.Append(scaleTarget.DOScale(collapsedScale, timeToScale));
            expandSequence.Join(DOVirtual.Float(defaultIntesity, 0, timeToScale, (value) =>
            {
                light.intensity = value;
            }));
            expandSequence.SetEase(ease);
            expandSequence.onComplete = () =>
            {
                titleText.enabled = false;
                outerTitleText.enabled = true;
                descriptionText.enabled = true;

                outerTitleTypewriter.TypeText();
            };
            
        }
    }
    #endregion


    private void Start()
    {
        if (interactable == null) interactable = GetComponent<XRBaseInteractable>();
        if (scaleTarget == null) scaleTarget = transform;

        outerTitleTypewriter = outerTitleText.transform.GetComponent<Typewriter>();
        descriptionTypewriter = descriptionText.transform.GetComponent<Typewriter>();

        outerTitleTypewriter.onFinish.AddListener(OuterTitleFinish);
    }

    public void SetExpanded(bool isExpanded, bool skipAnimation = false)
    {
        if (!skipAnimation)
        {
            IsExpanded = isExpanded;
            return;
        }

        this.isExpanded = isExpanded;

        if (propertyHelper != null)
        {
            MaterialPropertyBlock propertyBlock = propertyHelper.GetMaterialPropertyBlock();
            propertyBlock?.SetInt("_IsExpanded", isExpanded ? 1 : 0);
        }

        outerTitleTypewriter.isUntyping = isExpanded;
        outerTitleTypewriter.ResetCursor();

        positionLockOverride = isExpanded;
        light.enabled = isExpanded;
        scaleTarget.localScale = isExpanded ? expandedScale : collapsedScale;
        titleText.enabled = isExpanded;
        outerTitleText.enabled = !isExpanded;
        descriptionText.enabled = !isExpanded;
    }
    public void SetDescritpion(string text)
    {
        descriptionTypewriter.text = text;
    }
    public void RegisterSelectCallback()
    {

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

    private void OuterTitleFinish()
    {
        if (!isExpanded) return;

        titleText.enabled = true;
        outerTitleText.enabled = false;
        descriptionText.enabled = false;

        titleText.color = new Color(1, 1, 1, 0);

        expandSequence = DOTween.Sequence();
        expandSequence.Append(scaleTarget.DOScale(expandedScale, timeToScale).SetEase(ease));
        expandSequence.Join(DOVirtual.Float(0, defaultIntesity, timeToScale, (value) =>
        {
            light.intensity = value;
        }));
        expandSequence.Append(DOVirtual.Color(new Color(1, 1, 1, 0), Color.white, timeToScale, (value) =>
        {
            titleText.color = value;
        }));
        //expandSequence.onComplete = () =>
        //{
            
        //};
    }
}
