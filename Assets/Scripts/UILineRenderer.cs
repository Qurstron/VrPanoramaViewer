using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    public enum Type
    {
        Simple,     // simple linestrip
        Bezier,     // Bézier curve
        BSpline,    // B-Spline curve
        Single      // disconected lines
    }
    public Type type = Type.Simple;
    public float width = 2;
    public List<Vector2> points = new();
    public float inspectorHandleSize = 5;

    [Header("Curve Settings")]
    public float segmentFactor = 12.0f;
    public int segmentMin = 3;
    public int degree = 2;
    private float[] knoten;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null) return;
        if (points.Count < 2) return;

        if (type == Type.Single) DrawSingle(vh);
        else DrawConnected(vh);
    }

    private void DrawConnected(VertexHelper vh)
    {
        List<Vector2> points = GetPoints();

        Vector2 currentPoint = points[0];
        Vector2 nextPoint = points[1];
        Vector2 dir = nextPoint - currentPoint;
        Vector2 left = new Vector2(-dir.y, dir.x).normalized * width;
        UIVertex vertex = UIVertex.simpleVert;
        int skipVertices = 0;

        vertex.color = color;
        DrawLineCap(vh, vertex, currentPoint, left);

        for (int i = 2; i < points.Count; i++)
        {
            Vector2 lastPoint = currentPoint;
            currentPoint = nextPoint;
            nextPoint = points[i];

            Vector2 newDir = nextPoint - currentPoint;
            Vector2 newLeft = new Vector2(-newDir.y, newDir.x).normalized * width;

            try
            {
                Vector2 p1 = CalcIntersection(lastPoint + left, dir, nextPoint + newLeft, newDir);

                vertex.position = p1;
                vh.AddVert(vertex);
                vertex.position = 2 * currentPoint - p1; // currentPoint - (p1 - currentPoint)
                vh.AddVert(vertex);

                dir = newDir;
                left = newLeft;
            }
            catch
            {
                skipVertices++;
            }
        }

        DrawLineCap(vh, vertex, nextPoint, left);

        for (int i = 0; i < points.Count - (1 + skipVertices); i++)
        {
            int index = i * 2;
            vh.AddTriangle(index + 0, index + 1, index + 3);
            vh.AddTriangle(index + 3, index + 2, index + 0);
        }
    }
    private void DrawSingle(VertexHelper vh)
    {
        if (points.Count % 2 != 0) throw new ArgumentException("number of points must be divisble by 2");

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        for (int i = 0; i < points.Count - 1; i += 2)
        {
            Vector2 currentPoint = points[i];
            Vector2 dir = points[i + 1] - currentPoint;
            Vector2 left = new Vector2(-dir.y, dir.x).normalized * width;
            DrawLineCap(vh, vertex, currentPoint, left);

            currentPoint = points[i + 1];
            DrawLineCap(vh, vertex, currentPoint, left);
        }

        for (int i = 0; i < points.Count * 2; i += 4)
        {
            vh.AddTriangle(i + 0, i + 1, i + 3);
            vh.AddTriangle(i + 3, i + 2, i + 0);
        }
    }

    private List<Vector2> GetPoints()
    {
        List<Vector2> positions = new();
        int segCount;

        switch (type)
        {
            case Type.Bezier:
                segCount = GetSegmentCount();
                for (int i = 0; i < segCount; i++)
                {
                    float t = (float)i / (segCount - 1);
                    positions.Add(DeCasteljau(points, points.Count - 1, 0, t));
                }
                return positions;

            case Type.BSpline:
                int m = points.Count - 1;
                degree = Math.Max(1, Math.Min(degree, points.Count - 2));
                knoten = new float[degree + m + 2];

                for (int i = 0; i <= degree; i++)
                    knoten[i] = 0;
                for (int i = degree + 1; i <= m; i++)
                    knoten[i] = i - degree;
                for (int i = m + 1; i <= degree + m + 1; i++)
                    knoten[i] = m + 1 - degree;

                segCount = GetSegmentCount();
                float tDif = (knoten[m + 1] - knoten[degree]) / segCount;
                for (float t = knoten[degree]; t <= knoten[m + 1]; t += tDif)
                {
                    positions.Add(DeBoor(t));
                }
                return positions;

            case Type.Simple:
            default:
                return points;
        };
    }
    private int GetSegmentCount()
    {

        float accumulatedDistance = 0;
        Vector2 lastPoint = points[0];
        foreach (Vector2 point in points)
        {
            accumulatedDistance += Vector2.Distance(lastPoint, point);
            lastPoint = point;
        }
        Vector2 areaSize = new(points.Max(p => p.x) - points.Min(p => p.x), points.Max(p => p.y) - points.Min(p => p.y));
        float areaCoefficient = Mathf.Pow(areaSize.x * areaSize.y, .1f);

        int count = (int)(segmentFactor * areaCoefficient * accumulatedDistance / 100);
        return math.max(count, segmentMin);
    }
    private void DrawLineCap(VertexHelper vh, UIVertex vertex, Vector2 point, Vector2 left)
    {
        vertex.position = point;
        vertex.position += (Vector3)left;
        vh.AddVert(vertex);
        vertex.position = point;
        vertex.position -= (Vector3)left;
        vh.AddVert(vertex);
    }
    
    // https://stackoverflow.com/questions/563198/how-do-you-detect-where-two-line-segments-intersect
    private float Cross2D(Vector2 left, Vector2 right)
    {
        return left.x * right.y - left.y * right.x;
    }
    // throws exection if lines don't intersect
    private Vector2 CalcIntersection(Vector2 q, Vector2 qDir, Vector2 p, Vector2 pDir)
    {
        float qDis = Cross2D(q - p, pDir) / Cross2D(pDir, qDir);
        if (float.IsNaN(qDis) || float.IsInfinity(qDis)) throw new Exception("no intersection");
        return q + qDis * qDir;
    }

    private Vector2 DeCasteljau(List<Vector2> points, int r, int i, float t)
    {
        if (r == 0) return points[i];

        Vector2 p1 = DeCasteljau(points, r - 1, i, t);
        Vector2 p2 = DeCasteljau(points, r - 1, i + 1, t);

        return (1.0f - t) * p1 + t * p2;
    }
    private Vector2 DeBoor(float t)
    {
        float tkj;
        int m = points.Count - 1;
        Vector2[] d = new Vector2[m + 1];

        int i = 0;
        while (!(knoten[i] <= t && t < knoten[i + 1]) && i < m)
        {
            i++;
        }

        for (int k = i - degree; k <= i; k++)
        {
            d[k] = points[k];
        }

        for (int j = 1; j <= degree; j++)
        {
            for (int k = i; k >= i - degree + j; k--)
            {
                tkj = (t - knoten[k]) / (knoten[k + degree + 1 - j] - knoten[k]);
                d[k] = (1 - tkj) * d[k - 1] + tkj * d[k];
            }
        }

        return d[i];
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(UILineRenderer))]
public class UILineRendererEditor : Editor
{
    private UILineRenderer uiLineRenderer;

    private void OnEnable()
    {
        uiLineRenderer = (UILineRenderer)target;
        SceneView.duringSceneGui += OnSceneGUI;
    }
    void OnDestroy()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    private void OnSceneGUI(SceneView sv)
    {
        if (uiLineRenderer.inspectorHandleSize <= 0) return;

        Handles.color = Color.red;
        for (int i = 0; i < uiLineRenderer.points.Count; i++)
        {
            Vector2 newPos = Handles.FreeMoveHandle(uiLineRenderer.transform.position + (Vector3)uiLineRenderer.points[i], uiLineRenderer.inspectorHandleSize, Vector2.zero, Handles.CylinderHandleCap) - uiLineRenderer.transform.position;
            if (uiLineRenderer.points[i] != newPos)
            {
                Undo.RecordObject(uiLineRenderer, "Move point");
                uiLineRenderer.points[i] = newPos;
                uiLineRenderer.SetVerticesDirty();
            }
        }
    }
    override public void OnInspectorGUI()
    {
        switch (uiLineRenderer.type)
        {
            case UILineRenderer.Type.Simple:
                DrawUIExcluding("segmentFactor", "segmentMin", "degree");
                break;
            case UILineRenderer.Type.Single:
                DrawUIExcluding("segmentFactor", "segmentMin", "degree");
                break;
            case UILineRenderer.Type.Bezier:
                DrawUIExcluding("degree");
                break;
            case UILineRenderer.Type.BSpline:
            default:
                base.OnInspectorGUI();
                break;
        }
    }
    private void DrawUIExcluding(params string[] toExclude)
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, toExclude);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
