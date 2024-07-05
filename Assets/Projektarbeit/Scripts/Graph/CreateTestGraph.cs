using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using static JSONClasses;

public class CreateTestGraph : MonoBehaviour
{
    public GameObject nodeExpanededPrefab;
    public GameObject nodeCollabsedPrefab;

    void Start()
    {
        Node root = new();
        root.neighbors = new();
        root.displayName = "Root";
        root.color = "#BF37BD";

        Node n1 = new();
        n1.displayName = "Node 1 Test";
        n1.color = "#65C23C";
        Node n2 = new();
        n2.displayName = "Node 2 Test";
        n2.color = "#65C23C";

        root.neighbors.Add(n1);
        root.neighbors.Add(n2);
        SpawnGraph(root);


    }

    private void SpawnGraph(Node root)
    {
        int nodeCount = 1;
        Queue<Node> openList = new();
        root.neighbors.ForEach(n => openList.Enqueue(n));

        NodeProperties properties = Instantiate(nodeExpanededPrefab, transform).GetComponent<NodeProperties>();
        properties.gameObject.name = root.displayName;
        SetNodeProperties(root, properties);

        while (openList.Count > 0)
        {
            Node node = openList.Dequeue();

            properties = Instantiate(nodeCollabsedPrefab, transform).GetComponent<NodeProperties>();
            properties.gameObject.transform.localPosition = new Vector3(nodeCount * .5f, 0);
            properties.gameObject.name = node.displayName;
            SetNodeProperties(node, properties);
            node.neighbors?.ForEach(n => openList.Enqueue(n));

            nodeCount++;
        }
    }
    private void SetNodeProperties(Node node, NodeProperties nodeProperties)
    {
        if (ColorUtility.TryParseHtmlString(node.color, out UnityEngine.Color color))
        {
            //UnityEngine.Color c = nodeProperties.Color;
            //Debug.Log(color.r);
            nodeProperties.Color = color;
        }
        else
        {
            Debug.LogWarning($"could not parse color \"{node.color}\" for Node {node.displayName}");
        }
        nodeProperties.DisplayName = node.displayName;
    }
}
