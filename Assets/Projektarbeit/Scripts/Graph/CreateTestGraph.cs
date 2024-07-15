using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;
using UnityEngine.XR.Interaction.Toolkit;
using static JSONClasses;

public class CreateTestGraph : MonoBehaviour
{
    [Header("Prefabs")]
    //public GameObject nodeExpanededPrefab;
    public GameObject nodeCollabsedPrefab;
    public GameObject nodeLineVFXPrefab;
    [Header("Config")]
    public float materialInstesity = 3f;
    public float targetDistance = .5f;
    public float repelFactor = .01f;
    public float forceFactor = .01f;
    public float selectDuration = 1f;
    public float hideSpeed = 1f;

    private Queue<Node> openList = new();
    private HashSet<Node> closedList = new();
    private Transform graphParent = null;
    private Transform nodeParent = null;
    private Transform lineParent = null;
    private NodeProperties[] nodeProperties = new NodeProperties[0];
    private NodeProperties selectedNode = null;
    private Sequence selectSequence = null;
    private Sequence scaleSequence;
    private bool isVisible = true;
    private bool isSuspended = false;

    private void Start()
    {
        graphParent = SpawnEmpty("Content");
        lineParent = SpawnEmpty("Lines", graphParent);
        nodeParent = SpawnEmpty("Nodes", graphParent);

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

        root.neighbors.Add(new(n1, 1));
        root.neighbors.Add(new(n2, 1));
        n1.neighbors.Add(new(root, 1));
        n1.neighbors.Add(new(n3, 2));
        n2.neighbors.Add(new(root, 1));
        n2.neighbors.Add(new(n3, 2));
        n3.neighbors.Add(new(n1, 2));
        n3.neighbors.Add(new(n2, 2));

        SpawnGraph(root);
    }
    // https://citeseerx.ist.psu.edu/document?repid=rep1&type=pdf&doi=be33ebd01f336c04a1db20830576612ab45b1b9b
    private void Update()
    {
        if (isSuspended) return;

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
                Vector3 dif = node.TheoreticalPosition - neighbor.node.properties.TheoreticalPosition;
                node.Force -= dif * (dif.magnitude / (targetDistance * neighbor.metric));
            });
        }

        foreach (var node in nodeProperties)
        {
            node.transform.localPosition += node.Force * forceFactor;
            node.Force = Vector3.zero;
        }
    }

    // TODO: test
    //private void OnBecameVisible()
    //{
    //    isSuspended = false;
    //}
    //private void OnBecameInvisible()
    //{
    //    isSuspended = true;
    //}

    public bool GetIsGraphEnabled()
    {
        return isVisible;
    }
    public Sequence SetIsGraphEnabled(bool value)
    {
        if (isVisible == value) return null;

        isVisible = value;

        scaleSequence.Kill(false);
        scaleSequence = DOTween.Sequence();
        TweenerCore<Vector3, Vector3, VectorOptions> tween;
        if (isVisible)
        {
            graphParent.gameObject.SetActive(true);
            StartCoroutine(LineUpdateFixCorutine());
            tween = graphParent.DOScale(Vector3.one, hideSpeed * 2);
            tween.SetEase(Ease.OutSine);
            tween.onComplete = () =>
            {
                isSuspended = false;
            };
            scaleSequence.Append(tween);
            return scaleSequence;
        }

        isSuspended = true;
        tween = graphParent.DOScale(Vector3.zero, hideSpeed);
        tween.onComplete = () =>
        {
            graphParent.gameObject.SetActive(false);
        };

        return scaleSequence;
    }
    // the vfx propertybinder dosn't update correctly
    // after the graph has benn closed for a few secounds
    // to fix it toggle the lines in the next frame (current frame dosn't work)
    private IEnumerator LineUpdateFixCorutine()
    {
        yield return 0; // wait for next frame
        lineParent.gameObject.SetActive(false);
        lineParent.gameObject.SetActive(true);
        yield return null;
    }

    private Transform SpawnEmpty(string name, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        go.transform.parent = parent == null ? transform : parent;
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    // Creates the Graph from the root breath first
    // https://stackoverflow.com/questions/31247634/how-to-keep-track-of-depth-in-breadth-first-search
    private void SpawnGraph(Node root)
    {
        openList.Clear();
        closedList.Clear();

        List<NodeProperties> nodeProperties = new();
        NodeProperties properties = Instantiate(nodeCollabsedPrefab, nodeParent).GetComponent<NodeProperties>();
        int depth = 1;

        root.neighbors.ForEach(n => openList.Enqueue(n.node));
        openList.Enqueue(null);
        closedList.Add(root);

        properties.gameObject.name = root.displayName;
        SetNodeProperties(root, properties);
        root.gameObject = properties.gameObject;
        nodeProperties.Add(properties);
        selectedNode = properties;
        properties.SetExpanded(true, true);
        properties.interactable.activated.AddListener(SetSelectedNode);

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
            node.neighbors?.ForEach(n => openList.Enqueue(n.node));

            // spawn node
            properties = Instantiate(nodeCollabsedPrefab, nodeParent).GetComponent<NodeProperties>();
            properties.gameObject.transform.localPosition = depth * targetDistance * Random.onUnitSphere;
            properties.gameObject.name = node.name;
            SetNodeProperties(node, properties);
            node.gameObject = properties.gameObject;
            nodeProperties.Add(properties);
            properties.SetExpanded(false);
            properties.interactable.activated.AddListener(SetSelectedNode);

            node.neighbors.ForEach(n =>
            {
                if (!closedList.Contains(n.node)) return;
                SpawnLine(node, n.node);
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

    // gets called wenn a node has been selected
    private void SetSelectedNode(ActivateEventArgs args)
    {
        NodeProperties property = args.interactableObject.transform.GetComponent<NodeProperties>();
        if (selectedNode == property) return;

        var interactor = property.transform.GetComponent<XRBaseInteractable>().interactorsSelecting[0] as XRBaseInteractor;
        interactor.allowSelect = false;
        
        selectedNode.IsPositionLocked = false;
        selectedNode.IsExpanded = false;

        property.positionLockOverride = true;
        property.isForceDrop = true;

        selectSequence = DOTween.Sequence();
        selectSequence.Append(property.transform.DOMove(transform.position, selectDuration).SetEase(Ease.OutExpo));
        selectSequence.onUpdate = () =>
        {
            property.originalPos = property.transform.position - transform.position;
        };
        selectSequence.onComplete = () =>
        {
            interactor.allowSelect = true;
            interactor.allowHover = true;
            property.isForceDrop = false;
            property.IsExpanded = true;
        };

        selectedNode = property;
    }
}

// https://forum.unity.com/threads/assign-property-binder-targets-via-script.1093306/
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
