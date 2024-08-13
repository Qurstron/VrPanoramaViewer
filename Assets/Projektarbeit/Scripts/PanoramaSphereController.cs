using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using static JSONClasses;

public class PanoramaSphereController : MonoBehaviour
{
    public Transform positionParent;
    public Texture defaultTexture;
    public Renderer panoramaRenderer;
    public GameObject labelPrefab;
    public GameObject LinePrefab;
    public float radiusRatioLabel = 1.0f;
    public float radiusRatioLine = 1.0f;
    public float maxLineDistance = 10.0f;
    public float globalRotationOffset = 0;

    public Dictionary<string, byte[]> TextureData { get; set; }
    private float radiusLabel;
    private float radiusLine;
    private string currentTextureName;

    void Start()
    {
        radiusLabel = radiusRatioLabel * gameObject.GetNamedChild("Sphere").transform.localScale.x / 100;
        radiusLine = radiusRatioLine * gameObject.GetNamedChild("Sphere").transform.localScale.x / 100;
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: fix objects displaced on first frame
        gameObject.transform.position = positionParent.position;
    }

    // more of a test feature
    void OnApplicationQuit()
    {
        panoramaRenderer.material.mainTexture = defaultTexture;
    }

    public void SetApperance(NodeContent content)
    {
        foreach (Transform child in transform)
        {
            if (child.name == "Sphere") continue;
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
    }
    public void ReloadTexture()
    {
        if (!(panoramaRenderer.enabled = !string.IsNullOrEmpty(currentTextureName))) return;

        Texture2D texture = new(1, 1);
        texture.LoadImage(TextureData.GetValueOrDefault(currentTextureName));
        panoramaRenderer.material.mainTexture = texture;
    }

    public bool UpdateTexture(string texName, byte[] texData)
    {
        if (!TextureData.ContainsKey(texName)) return false;

        TextureData[texName] = texData;
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
        var labelObject = Instantiate(labelPrefab, gameObject.transform);
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
        GameObject lineObject = Instantiate(LinePrefab, gameObject.transform);
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
}
