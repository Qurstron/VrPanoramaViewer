using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using static JSONClasses;
using static JSONClasses.CategoryObject;

public class CreateTestGraph : MonoBehaviour
{
    public GameObject nodeExpanededPrefab;
    public GameObject nodeCollabsedPrefab;
    public GameObject nodeLineVFXPrefab;
    public float materialInstesity = 3f;
    public float targetDistance = .5f;
    public float repelFactor = .01f;
    //public float springFactor = .01f;
    public float forceFactor = .01f;

    private Queue<Node> openList = new();
    private HashSet<Node> closedList = new();
    private Transform nodeParent = null;
    private Transform lineParent = null;
    private NodeProperties[] nodeProperties = new NodeProperties[0];

    private void Start()
    {
        lineParent = SpawnEmpty("Lines");
        nodeParent = SpawnEmpty("Nodes");

        float intesityFactor = Mathf.Pow(2, materialInstesity);

        Node root = new();
        root.name = "root";
        root.neighbors = new();
        root.displayName = "Root";
        root.color = "#BF37BD";
        if (ColorUtility.TryParseHtmlString(root.color, out UnityEngine.Color color))
        {
            color *= intesityFactor;
            root.convertedColor = color.gamma;
        }

        Node n1 = new();
        n1.name = "n1";
        n1.displayName = "Node 1 Test";
        n1.color = "#65C23C";
        n1.neighbors = new();
        if (ColorUtility.TryParseHtmlString(n1.color, out color))
        {
            color *= intesityFactor;
            n1.convertedColor = color.gamma;
        }

        Node n2 = new();
        n2.name = "n2";
        n2.displayName = "Node 2 Test";
        n2.color = "#65C23C";
        n2.neighbors = new();
        if (ColorUtility.TryParseHtmlString(n2.color, out color))
        {
            color *= intesityFactor;
            n2.convertedColor = color.gamma;
        }

        Node n3 = new();
        n3.name = "n3";
        n3.displayName = "Node 3 Test";
        n3.color = "#33A9E0";
        n3.neighbors = new();
        if (ColorUtility.TryParseHtmlString(n3.color, out color))
        {
            color *= intesityFactor;
            n3.convertedColor = color.gamma;
        }

        root.neighbors.Add(n1);
        root.neighbors.Add(n2);
        n1.neighbors.Add(root);
        n1.neighbors.Add(n3);
        n2.neighbors.Add(root);
        n2.neighbors.Add(n3);
        n3.neighbors.Add(n1);
        n3.neighbors.Add(n2);

        SpawnGraph(root);
    }
    // https://citeseerx.ist.psu.edu/document?repid=rep1&type=pdf&doi=be33ebd01f336c04a1db20830576612ab45b1b9b
    private void Update()
    {
        // repel other nodes
        for (int i = 0; i < nodeProperties.Length - 1; i++)
        {
            NodeProperties node = nodeProperties[i];
            for (int j = i + 1; j < nodeProperties.Length; j++)
            {
                NodeProperties nextNode = nodeProperties[j];
                Vector3 dif = node.TheoreticalPosition - nextNode.TheoreticalPosition;
                float distance = dif.magnitude;
                //if (distance > targetDistance) continue;

                //Vector3 normal = dif.normalized;
                float repelForce = repelFactor * Mathf.Pow(targetDistance, 2) / Mathf.Pow(distance, 2);

                node.Force += dif * repelForce;
                nextNode.Force -= dif * repelForce;
            }
        }
        // spring neighbors
        for (int i = 0; i < nodeProperties.Length; i++)
        {
            NodeProperties node = nodeProperties[i];
            node.node.neighbors?.ForEach(neighbor =>
            {
                // TODO: use edge weigth instad of targetDistance
                Vector3 dif = node.TheoreticalPosition - neighbor.properties.TheoreticalPosition;
                node.Force -= dif * (dif.magnitude / targetDistance);
            });
        }

        foreach (var node in nodeProperties)
        {
            node.transform.position += node.Force * forceFactor;
            node.Force = Vector3.zero;
        }
    }

    private Transform SpawnEmpty(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = transform;
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    // https://stackoverflow.com/questions/31247634/how-to-keep-track-of-depth-in-breadth-first-search
    private void SpawnGraph(Node root)
    {
        openList.Clear();
        closedList.Clear();

        List<NodeProperties> nodeProperties = new();
        NodeProperties properties = Instantiate(nodeExpanededPrefab, nodeParent).GetComponent<NodeProperties>();
        int depth = 1;

        root.neighbors.ForEach(n => openList.Enqueue(n));
        openList.Enqueue(null);
        closedList.Add(root);

        properties.gameObject.name = root.displayName;
        SetNodeProperties(root, properties);
        root.gameObject = properties.gameObject;
        nodeProperties.Add(properties);

        while (openList.Count > 0)
        {
            Node node = openList.Dequeue();
            if (node == null)
            {
                depth++;
                openList.Enqueue(null);
                if (openList.Peek() == null) break;
                continue;
            }
            if (closedList.Contains(node)) continue;
            closedList.Add(node);
            node.neighbors?.ForEach(n => openList.Enqueue(n));

            // spawn node
            properties = Instantiate(nodeCollabsedPrefab, nodeParent).GetComponent<NodeProperties>();
            properties.gameObject.transform.localPosition = Random.onUnitSphere * depth * targetDistance;
            properties.gameObject.name = node.name;
            SetNodeProperties(node, properties);
            node.gameObject = properties.gameObject;
            nodeProperties.Add(properties);

            node.neighbors.ForEach(n =>
            {
                if (!closedList.Contains(n)) return;
                SpawnLine(node, n);
            });
        }

        this.nodeProperties = nodeProperties.ToArray();
    }
    private void SpawnLine(Node nodeA, Node nodeB)
    {
        GameObject go = Instantiate(nodeLineVFXPrefab, Vector3.zero, Quaternion.identity, lineParent);
        VisualEffect effect = go.GetComponent<VisualEffect>();
        VFXPropertyBinder linePropertyBinder = go.GetComponent<VFXPropertyBinder>();
        VFXPositionBinderCustom positionBinder = linePropertyBinder.AddPropertyBinder<VFXPositionBinderCustom>();
        Gradient gradient = new Gradient();

        positionBinder.Property = "PosStart";
        positionBinder.Target = nodeA.gameObject.transform;
        positionBinder = linePropertyBinder.AddPropertyBinder<VFXPositionBinderCustom>();
        positionBinder.Property = "PosEnd";
        positionBinder.Target = nodeB.gameObject.transform;

        gradient.colorKeys = new GradientColorKey[] { new GradientColorKey(nodeA.convertedColor.linear, 0), new GradientColorKey(nodeB.convertedColor.linear, 1) };
        effect.SetGradient("Gradient", gradient);
        effect.SetFloat("Seed", Random.value);
    }
    private void SetNodeProperties(Node node, NodeProperties nodeProperties)
    {
        node.properties = nodeProperties;

        nodeProperties.node = node;
        nodeProperties.Color = node.convertedColor;
        nodeProperties.DisplayName = node.displayName;
    }
}

public class VFXPositionBinderCustom : VFXBinderBase
{
    public string Property { get { return (string)m_Property; } set { m_Property = value; } }

    [VFXPropertyBinding("UnityEditor.VFX.Position", "UnityEngine.Vector3"), SerializeField, UnityEngine.Serialization.FormerlySerializedAs("m_Parameter")]
    protected ExposedProperty m_Property = "Position";
    public Transform Target = null;

    public override bool IsValid(VisualEffect component)
    {
        return Target != null && component.HasVector3(m_Property);
    }

    public override void UpdateBinding(VisualEffect component)
    {
        component.SetVector3(m_Property, Target.transform.position);
    }

    public override string ToString()
    {
        return string.Format("Position : '{0}' -> {1}", m_Property, Target == null ? "(null)" : Target.name);
    }
}
