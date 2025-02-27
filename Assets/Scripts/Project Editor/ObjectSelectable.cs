using DG.Tweening;
using JSONClasses;
using UnityEngine;
using static JSONClasses.AddOn;

[RequireComponent(typeof(Collider))]
public class ObjectSelectable
{
    public PanoramaSphereController panoramaSphereController;
    public Object3D object3D;
    public GameObject gameObject
    {
        get
        {
            return go;
        }
        set { go = value; }
    }
    public Transform transform
    {
        get { return go.transform; }
    }

    private GameObject go;
    private AddOn addOn;
    private bool isSelected = false;
    private Tween selectAnimation;
    // The original transform is needed, because the JsonTransform is in local space
    // and the Position of the object is driven by the glTF file
    private Vector3 ogPos;
    private Quaternion ogRot;
    private Vector3 ogScale;
    private GameObject labelObject;

    public bool IsSelected
    {
        get { return isSelected; }
        set
        {
            isSelected = value;
            GetOrCreateAddOn();

            if (isSelected)
            {
                Outline outline = GetOutline();
                var config = AppConfig.Config;

                OutlineWidth = config.defaultOutlineSize;
                OutlineColor = config.outlineColor;
                selectAnimation = DOVirtual.Color(
                    config.OutlineColor,
                    config.OutlinePulseColor,
                    config.selectPulseTime,
                    color =>
                    {
                        outline.OutlineColor = color;
                    }
                );
                selectAnimation.SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
            else
            {
                selectAnimation.Kill();
                if (addOn != null)
                {
                    OutlineColor = addOn.outlineColor;
                    OutlineWidth = addOn.outlineWidth ?? 0;
                }
                else if (gameObject.TryGetComponent(out Outline outline))
                {
                    GameObject.Destroy(outline);
                }
            }
        }
    }
    public AddOn AddOn
    {
        get { return addOn; }
        set
        {
            addOn = value;

            ogPos = transform.localPosition;
            ogRot = transform.localRotation;
            ogScale = transform.localScale;

            GetOutline().OutlineWidth = 0;
            if (addOn.outlineWidth != null)
                OutlineWidth = (float)addOn.outlineWidth;
            OutlineColor = addOn.outlineColor;

            if (addOn.transform != null)
                Position = addOn.transform.Translation;

            Label = addOn.label;
        }
    }

    #region Outline
    public float OutlineWidth
    {
        set
        {
            Outline outline = GetOutline();
            outline.OutlineWidth = value;
            CheckOutlineStatus();
        }
    }
    public string OutlineColor
    {
        set
        {
            Outline outline = GetOutline();
            try
            {
                outline.OutlineColor = QUtils.StringToColor(value);
            }
            catch
            {
                outline.OutlineColor = Color.white;
            }
            CheckOutlineStatus();
        }
    }
    public OutlineType OutlineType
    {
        set
        {
            Outline outline = gameObject.GetComponent<Outline>();
            outline.OutlineMode = value switch
            {
                OutlineType.Hidden => Outline.Mode.OutlineHidden,
                OutlineType.Visible => Outline.Mode.OutlineVisible,
                _ => Outline.Mode.OutlineAll,
            };
        }
    }

    public float OutlineWidthJson
    {
        get
        {
            if (AddOn == null) return 0;
            return AddOn.outlineWidth ?? 0;
        }
        set
        {
            GetOrCreateAddOn().outlineWidth = value;
            OutlineWidth = value;
        }
    }
    public string OutlineColorJson
    {
        get
        {
            if (AddOn == null) return "#FFFFFF";
            return AddOn.outlineColor;
        }
        set
        {
            GetOrCreateAddOn().outlineColor = value;
            OutlineColor = value;
        }
    }
    #endregion

    #region Transform
    public Vector3 Position
    {
        get { return transform.localPosition; }
        set { transform.localPosition = ogPos + value; }
    }
    public Quaternion Rotation
    {
        set { transform.localRotation = ogRot * value; }
    }
    public Vector3 Scale
    {
        set { transform.localScale = Vector3.Scale(ogScale, value); }
    }

    public Vector3 JsonPosition
    {
        get
        {
            if (AddOn == null) return Vector3.zero;
            if (addOn.transform == null) return Vector3.zero;
            return addOn.transform.Translation;
        }
        set
        {
            AddOn addOn = GetOrCreateAddOn();
            addOn.transform ??= new();
            addOn.transform.translation = new float[] { value.x, value.y, value.z };
            Position = value;
        }
    }
    public Quaternion JsonRotation
    {
        get
        {
            if (AddOn == null) return Quaternion.identity;
            if (addOn.transform == null) return Quaternion.identity;
            return addOn.transform.Rotation;
        }
        set
        {
            AddOn addOn = GetOrCreateAddOn();
            Vector3 eulerRot = value.eulerAngles;
            addOn.transform ??= new();
            addOn.transform.rotation = new float[] { eulerRot.x, eulerRot.y, eulerRot.z };
            Rotation = value;
        }
    }
    public Vector3 JsonScale
    {
        get
        {
            if (AddOn == null) return Vector3.one;
            if (addOn.transform == null) return Vector3.one;
            return addOn.transform.Scale;
        }
        set
        {
            AddOn addOn = GetOrCreateAddOn();
            addOn.transform ??= new();
            addOn.transform.scale = new float[] { value.x, value.y, value.z };
            Scale = value;
        }
    }
    #endregion

    #region Label
    public string Label
    {
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                if (labelObject != null)
                    GameObject.Destroy(labelObject);
                return;
            }

            GetOrCreateLabel().Text = value;
        }
    }

    public string LabelJson
    {
        get
        {
            if (AddOn == null) return null;
            return addOn.label;
        }
        set
        {
            GetOrCreateAddOn().label = value;
            Label = value;
        }
    }
    #endregion

    /// <summary>
    /// Enables or disables the Outline component based on the validity of width and color
    /// </summary>
    private void CheckOutlineStatus()
    {
        var outline = GetOutline();
        //gameObject.GetComponent<Outline>().enabled = OutlineWidthJson > 0 && OutlineColorJson != null;
        gameObject.GetComponent<Outline>().enabled = outline.OutlineWidth > 0;
    }
    private Outline GetOutline()
    {
        Outline outline = gameObject.GetComponent<Outline>();
        if (outline != null) return outline;

        outline = gameObject.AddComponent<Outline>();
        outline.OutlineWidth = AppConfig.Config.defaultOutlineSize;
        outline.OutlineColor = Color.black;

        return outline;
    }
    private ObjectLabel GetOrCreateLabel()
    {
        ObjectLabel label;

        if (labelObject == null)
        {
            labelObject = GameObject.Instantiate(panoramaSphereController.objectLabelPrefab, panoramaSphereController.inspectionTarget);
            label = labelObject.GetComponent<ObjectLabel>();
            label.cameraTarget = panoramaSphereController.Camera.transform;
            label.curveTarget = transform;
            label.lookTarget = panoramaSphereController.Camera.transform;
        }
        else
        {
            label = labelObject.GetComponent<ObjectLabel>();
        }

        return label;
    }
    private AddOn GetOrCreateAddOn()
    {
        if (addOn == null)
        {
            addOn = new()
            {
                path = GetGameObjectPath(gameObject)
            };

            object3D.addOns.Add(addOn);
            AddOn = addOn;
        }

        return addOn;
    }
    // Based on:
    // https://discussions.unity.com/t/how-can-i-get-the-full-path-to-a-gameobject/412
    public static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            if (obj.TryGetComponent<SceneRootContainer>(out var sceneRoot))
                break;
            path = obj.name + "/" + path;
        }
        return path;
    }


    private void OnDestroy()
    {
        if (labelObject != null) GameObject.Destroy(labelObject);
    }
}
