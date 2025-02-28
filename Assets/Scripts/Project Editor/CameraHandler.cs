using SFB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class CameraHandler : DragableChange, IScrollHandler, IPointerClickHandler, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private ProjectEditor editor;
    [SerializeField] private PanoramaSphereController panoramaSphereController;
    [Header("Camera")]
    [SerializeField] private Camera cameraObject;
    [SerializeField] private Transform cameraBoom;
    [SerializeField] private float dragSpeed = .5f;
    [SerializeField] private float scrollSpeed = .25f;
    [SerializeField] private float minScroll = .25f;
    [SerializeField] private float maxScroll = 1;
    [Range(0.0f, 90.0f)]
    [SerializeField] private float maxAngle = 80;
    [SerializeField] private bool lockXRot = false;
    [Tooltip("FOV change")]
    [SerializeField] private OnValueChangedEvent OnValueChanged = new();
    [Serializable] public class OnValueChangedEvent : UnityEvent<float> { }

    [Header("Interaction")]
    [SerializeField] private Toolbelt toolbelt;
    [SerializeField] private float selectToleanz = 1;
    public GameObject highlightObject;
    public Transform highlightTarget;

    private bool isMousInvalid = false;
    private bool wasMouseDraged = false;
    private Vector2 pointerAngle = Vector2.zero; // mouse Coords
    private Vector2 deltaAngle = Vector2.zero; // differenz between this and last frame
    private Vector2Int prevScreenSize = Vector2Int.zero;
    private Ray mouseRay = new();
    //private RenderTexture renderTexture;

    public float FOV { get { return cameraObject.fieldOfView; } }

    protected override void Start()
    {
        base.Start();

        panoramaSphereController.SetAreaSelectWidth((cameraObject.fieldOfView + 20) / 20000f);
        if (highlightTarget == null) highlightTarget = transform;
        if (cameraBoom == null) cameraBoom = cameraObject.transform;
    }
    protected override void OnDisable()
    {
        base.OnDisable();
        if (cameraObject == null) return;
        // reset cameara rotation, so a newly opened panorama dosn't inherit the previous rotation
        cameraObject.transform.rotation = Quaternion.identity;
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        if (!Application.isPlaying) return;
        if (cameraObject == null) return;

        Vector3[] corners = new Vector3[4];
        Vector2Int screenSize;

        GetComponent<RectTransform>().GetWorldCorners(corners);
        screenSize = new((int)(corners[2].x - corners[1].x), (int)(corners[1].y - corners[0].y));

        if (prevScreenSize.Equals(screenSize) || screenSize.x <= 0 || screenSize.y <= 0) return;
        prevScreenSize = screenSize;

        if (cameraObject.targetTexture)
        {
            cameraObject.targetTexture.Release();
            cameraObject.targetTexture.width = screenSize.x;
            cameraObject.targetTexture.height = screenSize.y;
            cameraObject.targetTexture.Create();
        }
    }

    public override void OnDeltaDrag(Vector2 deltaPos, PointerEventData eventData)
    {
        wasMouseDraged = true;
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (toolbelt.CurrentTool.GetType().GetInterfaces().Contains(typeof(IDragableAngle)) && !isMousInvalid)
                (toolbelt.CurrentTool as IDragableAngle).Drag(pointerAngle, deltaAngle);
            return;
        }

        // Rotate camera;
        deltaPos.y *= -1;
        deltaPos *= dragSpeed * cameraObject.fieldOfView / 100;
        Vector3 cameraRot = cameraBoom.rotation.eulerAngles;
        if (!lockXRot) cameraRot.x += deltaPos.y;
        cameraRot.y += deltaPos.x;

        if (cameraRot.x > 90) cameraRot.x -= 360;
        cameraRot.x = Mathf.Clamp(cameraRot.x, -maxAngle, maxAngle);

        cameraBoom.rotation = Quaternion.Euler(cameraRot);
    }

    public void OnScroll(PointerEventData eventData)
    {
        // orthographic projection is currently unused, but there is a chance it will be added back
        // in the future
        //cameraObject.orthographicSize -= eventData.scrollDelta.y * scrollSpeed * cameraObject.orthographicSize;
        //cameraObject.orthographicSize = Mathf.Clamp(cameraObject.orthographicSize, minScroll, maxScroll);
        cameraObject.fieldOfView -= eventData.scrollDelta.y * scrollSpeed;
        cameraObject.fieldOfView = Mathf.Clamp(cameraObject.fieldOfView, minScroll, maxScroll);

        OnValueChanged.Invoke(cameraObject.fieldOfView);

        panoramaSphereController.SetAreaSelectWidth((cameraObject.fieldOfView + 20) / 20000f);

        UpdateMouseCoords((Vector2)Input.mousePosition);
    }

    public void SetSize(float size)
    {
        //cameraObject.orthographicSize = size / 90;
        cameraObject.fieldOfView = size;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (wasMouseDraged)
        {
            wasMouseDraged = false;
            return;
        }
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (Physics.Raycast(mouseRay, out RaycastHit hitInfo))
        {
            ObjectSelectable selectable = hitInfo.collider.GetComponent<ObjectSelectableContainer>().selectable;
            if (selectable != null)
            {
                bool isClearing = !(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftShift));
                editor.ExecuteCommand(new SelectCommand(new List<ObjectSelectable>() { selectable }, isClearing));
            }

            return;
        }

        if (toolbelt.CurrentTool.GetType().GetInterfaces().Contains(typeof(IClickableAngle)))
            (toolbelt.CurrentTool as IClickableAngle).Click(pointerAngle);
    }
    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (eventData.button != PointerEventData.InputButton.Left) return;
        //pointerDownPos = eventData.position;
        if (toolbelt.CurrentTool.GetType().GetInterfaces().Contains(typeof(IDownableAngle)))
            (toolbelt.CurrentTool as IDownableAngle).Down(pointerAngle);
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (toolbelt.CurrentTool.GetType().GetInterfaces().Contains(typeof(IUpableAngle)))
            (toolbelt.CurrentTool as IUpableAngle).Up(pointerAngle);
        return;
    }
    public void OnPointerMove(PointerEventData eventData)
    {
        UpdateMouseCoords(eventData.position);
        if (isMousInvalid) return;

        if (toolbelt.CurrentTool.GetType().GetInterfaces().Contains(typeof(IMovable)))
            (toolbelt.CurrentTool as IMovable).Move(pointerAngle);
    }


    private void UpdateMouseCoords(Vector2 mouseScreenPos)
    {
        mouseRay = GetRayFromPos(mouseScreenPos);
        Vector2 coord = CalcAngleFromDirection(mouseRay.direction);
        // pointer is on the RenderTexture, but not on the sphere
        if (float.IsNaN(coord.x) || float.IsNaN(coord.y))
        {
            isMousInvalid = true;
            if (toolbelt.CurrentTool.GetType().GetInterfaces().Contains(typeof(IMouseLeftable)))
                (toolbelt.CurrentTool as IMouseLeftable).MouseLeft();
            return;
        }

        isMousInvalid = false;
        deltaAngle = coord - pointerAngle;
        pointerAngle = coord;
    }

    /// <summary>
    /// This is basically a copy from Camera.ScreenPointToRay, but it supports positions in a RectTransform.
    /// </summary>
    /// <param name="pos">Screen position</param>
    /// <returns>The ray from the camera</returns>
    public Ray GetRayFromPos(Vector2 pos)
    {
        var rectTrans = gameObject.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, pos, null, out Vector2 localPoint);

        Vector2 relative = new Vector2(
             localPoint.x / rectTrans.rect.width,
             localPoint.y / rectTrans.rect.height
        );

        // Angle in radians from the view axis
        // to the top plane of the view pyramid.
        float verticalAngle = 0.5f * Mathf.Deg2Rad * cameraObject.fieldOfView;

        // World space height of the view pyramid
        // measured at 1 m depth from the camera.
        float worldHeight = 2f * Mathf.Tan(verticalAngle);

        // Convert relative position to world units.
        Vector3 worldUnits = relative * worldHeight;
        worldUnits.x *= cameraObject.aspect;
        worldUnits.z = 1;

        // Rotate to match camera orientation.
        Vector3 direction = cameraObject.transform.rotation * worldUnits;

        // Output a ray from camera position, along this direction.
        return new Ray(cameraObject.transform.position, direction);
    }
    /// <summary>
    /// Calculates polar coordinates from a direction.
    /// </summary>
    /// <returns>The polar coordinates</returns>
    public Vector2 CalcAngleFromDirection(Vector3 dir)
    {
        Vector2 polar = new()
        {
            y = Mathf.Atan2(dir.x, dir.z),
            x = Mathf.Atan2(dir.y, new Vector2(dir.x, dir.z).magnitude)
        };
        polar *= Mathf.Rad2Deg;
        return polar;
    }
    public Vector2 ConvertWorldToUI(Vector3 worldPos)
    {
        // points on the opposite side of the camera have a point on screen
        // this "removes" those points up to a fov of 180°
        Vector3 relativePos = cameraObject.transform.position - worldPos;
        if (Vector3.Dot(relativePos, cameraObject.transform.forward) > 0) return new Vector2(-1, -1) * 100;
        return cameraObject.WorldToScreenPoint(worldPos);
    }

    /// <summary>
    /// Takes a screenshot of the viewport. UI not included.
    /// </summary>
    public void SaveRenderTexture()
    {
        RenderTexture renderTexture = cameraObject.targetTexture;
        string fileName = StandaloneFileBrowser.SaveFilePanel("Save Screenshot", Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), $"VR-Panorama {DateTime.Now:d MMM yyyy HH-mm-ss}", "png");
        if (string.IsNullOrEmpty(fileName)) return;

        Texture2D tex = new(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        RenderTexture.active = renderTexture;
        tex.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        File.WriteAllBytes(fileName, tex.EncodeToPNG());
        Debug.Log("Screenshot taken: " + fileName);
    }
    /// <summary>
    /// Rotates the camera to the average of the points.
    /// </summary>
    public void FocusAngel(Vector2 angle)
    {
        angle.x *= -1;
        cameraBoom.rotation = Quaternion.Euler(angle);
    }
    /// <summary>
    /// Sets the position of the camera boom to pos.
    /// </summary>
    public void FocusPosition(Vector3 pos)
    {
        cameraBoom.position = pos;
    }
}
