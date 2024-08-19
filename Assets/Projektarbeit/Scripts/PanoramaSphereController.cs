using GLTFast;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using static JSONClasses;

public class PanoramaSphereController : MonoBehaviour
{
    [Header("Sphere Specific")]
    public Transform positionParent;
    public Texture defaultTexture;
    public Renderer panoramaRenderer;
    public GameObject labelPrefab;
    public GameObject LinePrefab;
    public float radiusRatioLabel = 1.0f;
    public float radiusRatioLine = 1.0f;
    public float maxLineDistance = 10.0f;
    public float globalRotationOffset = 0;

    [Header("3D Object Specific")]
    public GameObject inspectionFloor;
    public Transform inspectionTarget;
    public GameObject label3DPrefab;

    public Config config;
    public Dictionary<string, byte[]> ContentData { get; set; }
    public Dictionary<string, GltfImport> Gltfs {  get; set; }

    private float radiusLabel;
    private float radiusLine;
    private string currentTextureName;
    private Transform dynamicContent;

    void Start()
    {
        inspectionFloor.SetActive(false);

        radiusLabel = radiusRatioLabel * gameObject.GetNamedChild("Sphere").transform.localScale.x / 100;
        radiusLine = radiusRatioLine * gameObject.GetNamedChild("Sphere").transform.localScale.x / 100;

        dynamicContent = new GameObject("Dynamic Content").transform;
        dynamicContent.transform.parent = transform;
        dynamicContent.transform.localPosition = Vector3.zero;
    }
    void Update()
    {
        panoramaRenderer.transform.position = positionParent.position;
    }

    // more of a test feature
    void OnApplicationQuit()
    {
        panoramaRenderer.material.mainTexture = defaultTexture;
    }

    public async void SetApperance(NodeContent content)
    {
        inspectionFloor.SetActive(false);
        foreach (Transform child in dynamicContent)
        {
            Destroy(child.gameObject);
        }
        foreach (Transform child in inspectionTarget)
        {
            Destroy(child.gameObject);
        }
        currentTextureName = content.texture;
        ReloadTexture();

        if (content == null) return;

        gameObject.GetNamedChild("Sphere").transform.rotation = Quaternion.Euler(-90, content.latitudeOffset.GetValueOrDefault(), 0);

        if (content.labels != null)
        {
            foreach (NodeContent.Label label in content.labels)
            {
                CreateLable(label);
            }
        }
        if (content.lines != null)
        {
            foreach (NodeContent.Line line in content.lines)
            {
                if (line.coords == null)
                {
                    Debug.LogWarning("Found line with no coords! this is probably an Config error");
                    continue;
                }
                CreateLine(line);
            }
        }
        if (content.objects != null)
        {
            inspectionFloor.SetActive(true);
            foreach (var obj in content.objects)
            {
                await LoadGltfBinaryFromMemory(obj);
            }
        }
    }
    public void ReloadTexture()
    {
        if (!(panoramaRenderer.enabled = !string.IsNullOrEmpty(currentTextureName)))
        {
            inspectionFloor.SetActive(true);
            return;
        }

        Texture2D texture = new(1, 1);
        texture.LoadImage(ContentData.GetValueOrDefault(currentTextureName));
        panoramaRenderer.material.mainTexture = texture;
    }

    public bool UpdateTexture(string texName, byte[] texData)
    {
        if (!ContentData.ContainsKey(texName)) return false;

        ContentData[texName] = texData;
        if (currentTextureName.Equals(texName)) ReloadTexture();

        return true;
    }

    private Vector3 convertArrayToPos(float[] coord, float radius, bool flipCoords = false)
    {
        if (coord.Length != 2) throw new Exception("invalid coords");

        float lambda = coord[0] * Mathf.Deg2Rad;
        float phi = coord[1] * Mathf.Deg2Rad;
        if (flipCoords)
        {
            float buf = lambda;
            lambda = phi;
            phi = buf;
        }

        Vector3 result = new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(lambda),
            Mathf.Sin(lambda),
            Mathf.Cos(phi) * Mathf.Cos(lambda)
            );

        return result * radius;
    }
    private GameObject CreateLable(NodeContent.Label label)
    {
        var labelObject = Instantiate(labelPrefab, dynamicContent);
        labelObject.transform.localPosition = convertArrayToPos(label.pos, radiusLabel);
        labelObject.transform.localRotation = Quaternion.Euler(-label.pos[0], label.pos[1], 0);

        string labelText = "";
        if (!string.IsNullOrEmpty(label.header))
        {
            labelText = $"<size=0.8><u>{label.header}</u></size>";
            if (!string.IsNullOrEmpty(label.header)) labelText += "\n";
        }
        labelText += label.content;
        labelObject.GetNamedChild("Text").GetComponent<TMP_Text>().text = labelText;

        return labelObject;
    }
    private GameObject CreateLine(NodeContent.Line line)
    {
        GameObject lineObject = Instantiate(LinePrefab, dynamicContent);
        LineRenderer lineRenderer = lineObject.GetComponentInChildren<LineRenderer>();
        List<Vector3> positions = new();
        Vector3 start;
        Vector3 end = convertArrayToPos(line.coords.First(), radiusLine, line.flipcoords);

        for (int i = 1; i < line.coords.Count; i++)
        {
            positions.Add(end);
            start = end;
            end = convertArrayToPos(line.coords[i], radiusLine, line.flipcoords);

            float distance = Vector3.Distance(start, end);
            if (distance < maxLineDistance) continue;

            float step = 1 / (distance / maxLineDistance);
            for (float f = step; f < 1; f += step)
            {
                positions.Add(Vector3.Slerp(start, end, f));
            }
        }
        positions.Add(end);

        lineRenderer.positionCount = positions.Count;
        lineRenderer.SetPositions(positions.ToArray());

        lineRenderer.widthCurve = new AnimationCurve(new Keyframe[] { new(0, line.width) });

        Color color;
        if (ColorUtility.TryParseHtmlString(line.color, out color))
        {
            Gradient g = new Gradient();
            g.SetKeys(new GradientColorKey[] { new GradientColorKey(color, 0) }, new GradientAlphaKey[] { new GradientAlphaKey(1, 0) });
            lineRenderer.colorGradient = g;
        }

        return lineObject;
    }
    private async Task<GameObject> LoadGltfBinaryFromMemory(Object3D obj)
    {
        if (!Gltfs.ContainsKey(obj.file))
        {
            Debug.LogError($"Could not find gltf: {obj.file} in memory");
            return null;
        }
        GltfImport gltf = Gltfs[obj.file];

        GameObject go = new GameObject(gltf.GetSceneName(0));
        go.transform.parent = inspectionTarget;
        go.transform.SetLocalPositionAndRotation(obj.transform.Translation, obj.transform.Rotation);
        go.transform.localScale = obj.transform.Scale;

        if (!await gltf.InstantiateMainSceneAsync(go.transform))
        {
            Debug.LogError($"could not instaniate gltf scene: {gltf.GetSceneName(0)}");
            Destroy(go);
            return null;
        }

        //var animations = gltf.GetAnimationClips();
        //Animator a;
        Debug.Log($"gltf scene: {gltf.GetSceneName(0)} loaded");
        return go;
    }
    private GameObject CreateLabel3D(GameObject go, AddOn.Label label)
    {
        return null;
    }
}
