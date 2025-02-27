using DG.Tweening;
using GLTFast;
using JSONClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Color = UnityEngine.Color;
using Label = JSONClasses.Label;

[RequireComponent(typeof(IDeferAgent))]
public class PanoramaSphereController : ConfigActor
{
    [SerializeField] private Texture2D defaultTexture;
    [SerializeField] private GameObject labelPrefab;
    [SerializeField] private GameObject LinePrefab;
    [SerializeField] private Camera overrideCamera;
    [SerializeField] private Material skyboxMat;
    public Transform appearanceTarget;
    public Transform inspectionTarget;
    [SerializeField] private CoolSlider exposureSlider;
    [SerializeField] private float radiusRatioLine = 1.0f;
    [SerializeField] private float maxLineDistance = 10.0f;
    [SerializeField] private float globalRotationOffset = 0;
    public GameObject objectLabelPrefab;
    public UnityEvent<List<WorldSelectable>> OnApperanceChange;
    public UnityEvent<List<SceneRoot>> OnObjectChange;
    public float objectScale = .1f;
    public float Radius
    {
        get { return radiusRatioLine; }
    }
    public Camera Camera { get; private set; }

    private NodeContent currentFullNodeContent;
    private float radiusLine;
    private static float radius = 0;
    //private Transform camera;
    private readonly List<WorldSelectable> apperenceObjects = new();
    private readonly List<SceneRoot> sceneRoots = new();
    private readonly Dictionary<MergeSubject, GameObject> worldSelectableMap = new();
    private GameObject areaSelectLine;
    private string currentTexture;

    void Start()
    {
        radiusLine = radiusRatioLine * transform.Find("Sphere").transform.localScale.x / 100;
        radius = radiusLine;

        if (overrideCamera != null)
            Camera = overrideCamera;
        else
            Camera = GetComponentInChildren<Camera>();

        areaSelectLine = Instantiate(LinePrefab, gameObject.transform);
        areaSelectLine.GetComponentInChildren<LineRenderer>().loop = true;
        areaSelectLine.SetActive(false);


        LineRenderer lineRenderer = areaSelectLine.GetComponentInChildren<LineRenderer>();
        Tween tween = DOVirtual.Float(1, 0, 1, f =>
        {
            Gradient g = new();
            Color color = Color.Lerp(Color.white, Color.yellow, f);
            g.SetKeys(new GradientColorKey[] { new(color, 0) }, new GradientAlphaKey[] { new(1, 0) });
            lineRenderer.colorGradient = g;
        });
        tween.SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
    void Update()
    {
        foreach (var worldSelectable in apperenceObjects)
        {
            if (worldSelectable == null) continue; // a bit hacky, but it works
            if (worldSelectable is not LabelComponent) continue;

            worldSelectable.transform.rotation = Camera.transform.rotation;
        }
    }
#if UNITY_EDITOR
    void OnApplicationQuit()
    {
        // Changes to a Material will not be reset on quit,
        // so we need to manually reset it
        SetExposure(1);
        RenderSettings.skybox.mainTexture = defaultTexture;

        ClearObjects();
    }
#endif
    protected override void OnContextChange()
    {
        ResetSphere();
        Context.OnNodeContentChange.AddListener(async () => await UpdateApperance());
    }

    public async Task UpdateApperance()
    {
        currentFullNodeContent = Context.FullNodeContent;

        ClearObjects();
        if (!(currentTexture == currentFullNodeContent.texture))
        {
            currentTexture = currentFullNodeContent.texture;
            await ReloadTexture();
        }

        foreach (ReflectionProbe probe in GetComponentsInChildren<ReflectionProbe>())
        {
            probe.RenderProbe();
        }

        if (currentFullNodeContent == null)
        {
            OnApperanceChange.Invoke(apperenceObjects);
            OnObjectChange.Invoke(sceneRoots);
            return;
        }

        //transform.Find("Sphere").transform.rotation = Quaternion.Euler(-90, currentFullNodeContent.latitudeOffset.GetValueOrDefault(), 0);

        if (currentFullNodeContent.labels != null)
        {
            foreach (Label label in currentFullNodeContent.labels)
            {
                CreateLabel(label);
            }
        }
        if (currentFullNodeContent.lines != null)
        {
            foreach (Line line in currentFullNodeContent.lines)
            {
                CreateLine(line);
            }
        }
        if (currentFullNodeContent.objects != null)
        {
            foreach (var obj in currentFullNodeContent.objects)
            {
                await LoadGltfBinaryFromMemory(obj);
            }
        }

        OnApperanceChange.Invoke(apperenceObjects);
        OnObjectChange.Invoke(sceneRoots);
    }
    public async Task ReloadTexture()
    {
        RenderSettings.skybox.SetFloat("_Rotation", 90 + (currentFullNodeContent.latitudeOffset ?? 0));
        if (string.IsNullOrEmpty(currentFullNodeContent.texture))
        {
            //RenderSettings.skybox.mainTexture = defaultTexture;
            skyboxMat.mainTexture = defaultTexture;
            return;
        }

        StartCoroutine(LagacyLoadTexture("file://" + Path.Combine(Context.Config.path, currentFullNodeContent.texture)));
        //using UnityWebRequest www = UnityWebRequestTexture.GetTexture("file://" + Path.Combine(Context.Config.path, currentFullNodeContent.texture));
        //await www.SendWebRequest();
        //if (www.result == UnityWebRequest.Result.Success)
        //{
        //    skyboxMat.mainTexture = DownloadHandlerTexture.GetContent(www);
        //}
    }
    public void SetExposure(float exposure)
    {
        RenderSettings.skybox.SetFloat("_Exposure", exposure);
    }
    public void SetApperanceActive(bool isUnactive)
    {
        appearanceTarget.gameObject.SetActive(!isUnactive);
    }
    public void SetShowcaseActive(bool isUnactive)
    {
        inspectionTarget.gameObject.SetActive(!isUnactive);
    }
    private void ResetSphere()
    {
        ClearObjects();
        currentTexture = null;

        if (exposureSlider != null)
            exposureSlider.SetValueDirect(1);
        RenderSettings.skybox.SetFloat("_Exposure", 1);
        RenderSettings.skybox.SetFloat("_Rotation", 90);
        RenderSettings.skybox.mainTexture = defaultTexture;

        //Transform cameraTransform = GetComponentInChildren<Camera>().transform;
        //cameraTransform.parent.rotation = Quaternion.identity;
        //cameraTransform.rotation = Quaternion.identity;
    }
    private void ClearObjects()
    {
        apperenceObjects.Clear();
        sceneRoots.Clear();
        worldSelectableMap.Clear();

        foreach (Transform child in appearanceTarget)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in inspectionTarget)
        {
            Destroy(child.gameObject);
        }
    }

    public GameObject CreateLabel(Label label)
    {
        GameObject labelObject = Instantiate(labelPrefab, appearanceTarget);
        LabelComponent labelComponent = new() { gameObject = labelObject };
        float scale = objectScale * radius;

        labelObject.transform.localScale = new Vector3(scale, scale, scale);
        labelObject.transform.localRotation = Quaternion.Euler(-label.pos[0], label.pos[1], 0); // will be overriden, but just in case

        QUtils.GetOrAddComponent<WorldSelectableContainer>(labelObject).selectable = labelComponent;
        labelComponent.sphereController = this;
        labelComponent.Label = label;

        apperenceObjects.Add(labelComponent);
        worldSelectableMap.Add(labelComponent.Label, labelObject);
        return labelObject;
    }
    public GameObject CreateLine(Line line)
    {
        if (line.coords == null) return null; // probably an Config error if this is true

        GameObject lineObject = Instantiate(LinePrefab, appearanceTarget);
        LineComponent lineComponent = new() { gameObject = lineObject };// lineObject.GetComponent<LineComponent>();

        QUtils.GetOrAddComponent<WorldSelectableContainer>(lineObject).selectable = lineComponent;
        lineComponent.sphereController = this;
        lineComponent.Line = line;

        apperenceObjects.Add(lineComponent);
        worldSelectableMap.Add(lineComponent.Line, lineObject);
        return lineObject;
    }

    public List<WorldSelectable> GetApperenceObject()
    {
        apperenceObjects.RemoveAll(go => go == null); // remove destroyed GameObjects
        return apperenceObjects;
    }
    public GameObject GetGameObject(MergeSubject subject)
    {
        if (worldSelectableMap.TryGetValue(subject, out GameObject go))
        {
            return go;
        }

        Debug.LogError("Unable to locate GameObject for WorldSelectable");
        return null;
    }

    public void setAreaSelectLine(List<Vector2> points)
    {
        if (points == null)
        {
            areaSelectLine.SetActive(false);
            return;
        }

        areaSelectLine.SetActive(true);

        // the line loop will close it selft, without regards to the maxLineDistance
        // so an addition point will be appended
        // the first point can't be duplicated, because that would messup the line
        float maxAngleDistance = MathF.Atan(maxLineDistance); // max line distance but in angle
        Vector2 angleDir = points.First() - points.Last();
        if (angleDir.magnitude > maxAngleDistance)
        {
            points.Add(points.First() - angleDir.normalized * maxAngleDistance);
        }

        LineRenderer lineRenderer = areaSelectLine.GetComponentInChildren<LineRenderer>();
        List<Vector3> positions = CalcPoints(points.ToArray(), LineStrategy.linearPath);

        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());
    }
    public void SetAreaSelectWidth(float width)
    {
        if (areaSelectLine == null) return; // fixes a wierd bug on closing the application
        width *= radiusLine;
        areaSelectLine.GetComponentInChildren<LineRenderer>().widthCurve = new AnimationCurve(new Keyframe[] { new(0, width) });
    }

    /// <summary>
    /// Calculates the cartesian path coordinates based on the polar coordinates with a LineStrategy
    /// </summary>
    /// <remarks>The return list and the angles list may have a different size</remarks>
    /// <returns>Null if angles is empty</returns>
    public List<Vector3> CalcPoints(Vector2[] angles, LineStrategy strategy)
    {
        if (angles.Length <= 0) return new();

        List<Vector3> positions = new();
        Vector3 start;
        Vector3 end = convertArrayToPos(angles[0]);
        Vector2 startAngle;
        Vector2 endAngle = angles[0];

        for (int i = 1; i < angles.Length; i++)
        {
            positions.Add(end);
            start = end;
            end = convertArrayToPos(angles[i]);
            startAngle = endAngle;
            endAngle = angles[i];

            float distance = Vector2.Distance(startAngle, endAngle);
            if (distance < maxLineDistance) continue;
            float step = maxLineDistance / distance;
            for (float t = step; t < 1; t += step)
            {
                positions.Add(strategy.Interpolate(startAngle, endAngle, t));
            }
        }
        positions.Add(end);

        return positions;
    }

    /// <summary>
    /// polar to cartesian coordinates
    /// </summary>
    public static Vector3 convertArrayToPos(Vector2 coord)
    {
        float lambda = coord.x * Mathf.Deg2Rad;
        float phi = coord.y * Mathf.Deg2Rad;

        Vector3 result = new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(lambda),
            Mathf.Sin(lambda),
            Mathf.Cos(phi) * Mathf.Cos(lambda)
            );

        return result * radius;
    }
    /// <summary>
    /// cartesian to polar coordinates
    /// </summary>
    public static Vector2 convertPosToAngle(Vector3 pos)
    {
        return new(MathF.Acos(pos.y), MathF.Atan2(pos.z, pos.x));
    }

    /// <summary>
    /// Loads and instantiate the main scene of the gltf binary described in obj
    /// </summary>
    /// <returns>The scene root</returns>
    public async Task<GameObject> LoadGltfBinaryFromMemory(Object3D obj)
    {
        if (!Context.Config.contentData.ContainsKey(obj.file))
        {
            Debug.LogError($"Could not find gltf: {obj.file} in memory");
            return null;
        }

        string path = Path.Combine(Context.Config.path, obj.file);
        ImportSettings settings = new()
        {
            GenerateMipMaps = true,
            AnisotropicFilterLevel = 3,
            NodeNameMethod = NameImportMethod.Original,
            AnimationMethod = AnimationMethod.Mecanim
        };
        var gltf = new GltfImport(deferAgent: GetComponent<IDeferAgent>());
        // https://stackoverflow.com/questions/75613904/unity-gltfast-cant-load-my-gltf-file-with-loadgltfbinary-function
        bool success = await gltf.Load(Context.Config.contentData[obj.file], new Uri(path), settings);
        if (!success)
        {
            Debug.LogError($"could not load gltf scene: {gltf.GetSceneName(0)}");
            return null;
        }

        GameObject go = new(obj.name);
        go.transform.parent = inspectionTarget;
        go.transform.SetLocalPositionAndRotation(obj.transform.Translation, obj.transform.Rotation);
        go.transform.localScale = obj.transform.Scale;

        if (!await gltf.InstantiateMainSceneAsync(new CustomGameObjectInstantiator(gltf, go.transform, this, obj)))
        {
            Debug.LogError($"could not instaniate gltf scene: {gltf.GetSceneName(0)} in {obj.name}");
            Destroy(go);
            gltf.Dispose();
            return null;
        }

        SceneRoot root = new() { gameObject = go };
        root.sphereController = this;
        root.Scene = obj;
        QUtils.GetOrAddComponent<SceneRootContainer>(go).selectable = root;
        sceneRoots.Add(root);
        worldSelectableMap.Add(root.Scene, go);

        foreach (AddOn addon in obj.addOns)
        {
            if (Context.currentNodeContent.excludes.Contains(addon.unquieID))
                continue;
            Transform addonTarget = go.transform.Find(addon.path);

            if (addonTarget == null)
            {
                Debug.LogError($"Unable to find path {addon.path} in {obj}");
                continue;
            }

            var objectSelectable = addonTarget.GetComponent<ObjectSelectableContainer>().selectable;
            objectSelectable.AddOn = addon;
            addon.object3D = obj;
        }

        return go;
    }

    private IEnumerator LagacyLoadTexture(string file)
    {
        using UnityWebRequest www = UnityWebRequestTexture.GetTexture(file);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            skyboxMat.mainTexture = DownloadHandlerTexture.GetContent(www);
            yield return null;
            foreach (ReflectionProbe probe in GetComponentsInChildren<ReflectionProbe>())
            {
                probe.RenderProbe();
            }
        }
    }
}
