using TMPro;
using UnityEngine;

public class ObjectLabel : MonoBehaviour
{
    [Tooltip("The Transform where the curve enters the label")]
    [SerializeField] private Transform staticCurveEndTarget;
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("How much the controll points are accentuated by the difference in position of the end points")]
    [SerializeField] private float archFactor = .5f;
    [Tooltip("The Transform that the label is pointing at")]
    public Transform curveTarget;
    public Transform lookTarget;
    public Transform cameraTarget;
    public float armLength = 4f;

    private Vector3 lastPos = Vector3.negativeInfinity;
    private Vector3[] controllPoints = new Vector3[5];

    public string Text
    {
        get
        {
            return GetComponentInChildren<TMP_Text>().text;
        }
        set
        {
            GetComponentInChildren<TMP_Text>().text = value;
        }
    }

    private void Awake()
    {
        lineRenderer.positionCount = controllPoints.Length;
    }
    private void Update()
    {
        if (lookTarget == null) return;

        transform.localPosition = curveTarget.localPosition;
        Vector3 dir = cameraTarget.position - transform.position;
        Vector3 rotated = Quaternion.Euler(0, -90, 0) * dir;
        rotated.y = 0;
        transform.localPosition = rotated.normalized * armLength + Vector3.up * armLength;

        transform.LookAt(2 * transform.position - cameraTarget.position);

        Vector3 dif = curveTarget.localPosition - staticCurveEndTarget.localPosition;
        dif *= archFactor;

        controllPoints[0] = staticCurveEndTarget.position;
        controllPoints[1] = staticCurveEndTarget.position - staticCurveEndTarget.right * archFactor;
        controllPoints[2] = staticCurveEndTarget.position + Vector3.Scale(Vector3.right, dif);
        controllPoints[3] = curveTarget.position + Vector3.Scale(Vector3.down, dif);
        controllPoints[4] = curveTarget.position;

        int segCount = 10;
        lineRenderer.positionCount = segCount;
        for (int i = 0; i < segCount; i++)
        {
            float t = (float)i / (segCount - 1);
            lineRenderer.SetPosition(i, DeCasteljau(controllPoints, controllPoints.Length - 1, 0, t));
        };
    }

    private Vector3 DeCasteljau(Vector3[] points, int r, int i, float t)
    {
        if (r == 0) return points[i];

        Vector3 p1 = DeCasteljau(points, r - 1, i, t);
        Vector3 p2 = DeCasteljau(points, r - 1, i + 1, t);

        return (1.0f - t) * p1 + t * p2;
    }
}
