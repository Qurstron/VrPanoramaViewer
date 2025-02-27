using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using JSONClasses;
using Random = System.Random;
using Unity.Mathematics;

[RequireComponent(typeof(UILineRenderer))]
[RequireComponent(typeof(ToggleGroup))]
public class GraphNavigator : ConfigActor
{
    [SerializeField] private GameObject nodeButtonPrefab;
    [SerializeField] private NodeColorField nodeColorField;
    [SerializeField] private UILineRenderer previewLine;
    [Header("Node")]
    [SerializeField] private float targetDistance = .5f;
    [SerializeField] private float repelFactor = .01f;
    [SerializeField] private float forceFactor = .01f;
    [SerializeField] private float equilibriumEpsilon = .01f;
    [SerializeField] private float maxForce = 1;
    [SerializeField] private float scrollSpeed = 1f;
    [SerializeField] private float maxForceDistance = 100;
    [SerializeField] private float previewAlpha = .5f;
    [Header("Edge")]
    [SerializeField] private float newEdgeSnapDistance = 100;
    [SerializeField] private Color newEdgeColor;
    [SerializeField] private Color deleteEdgeColor;
    [SerializeField] private Color highlightColor;

    private readonly Dictionary<Node, GraphButton> nodeGraphButtons = new();
    private readonly List<Tuple<GraphButton, GraphButton>> highlightedEdges = new();
    private GraphButton currentActiveButton;
    private Tuple<Node, Node> hoveredEdge;
    private bool wasDraged = false;
    private bool isHovered = false;
    private int hoveredButtons = 0;
    private Random random = new();
    private UILineRenderer lineRenderer;
    private GraphButton loseEnd;
    private Vector3 startMousePos = Vector3.zero;
    private UILineRenderer edgeHighlight;
    private Dictionary<Node, GraphButton>.ValueCollection GraphButtons
    {
        get { return nodeGraphButtons.Values; }
    }

    private void Awake()
    {
        GameObject copyLineRenderer = Instantiate(previewLine.gameObject, transform);
        copyLineRenderer.name = "Edge Highlight";
        copyLineRenderer.transform.SetAsFirstSibling();
        edgeHighlight = copyLineRenderer.GetComponent<UILineRenderer>();
        edgeHighlight.color = highlightColor;

        lineRenderer = GetComponent<UILineRenderer>();
        nodeColorField.onFieldChange.AddListener(color =>
        {
            GraphButton gb = nodeGraphButtons.GetValueOrDefault(Context.currentNode);
            gb.UpdateColor();
        });
    }
    private void Update()
    {
        if (GraphButtons.Count < 0) return;

        foreach (var node in nodeGraphButtons.Keys)
        {
            nodeGraphButtons[node].gameObject.SetActive(Context.Config.nodes.Contains(node));
        }
        List<GraphButton> buttons = GraphButtons.ToList();
        buttons.RemoveAll(b => !b.gameObject.activeSelf);

        float scale = transform.root.localScale.x;
        int maxNodes = Math.Min(buttons.Count, (int)AppConfig.Config.maxNodesForceDirectedPerUpdate);
        // Creates a array of indices in random order with an size of a maximum of maxNodesForceDirectedPerUpdate
        // This is used to randomize the updated nodes
        int[] indices = Enumerable.Range(0, buttons.Count).OrderBy(i => random.Next()).ToList().GetRange(0, maxNodes).ToArray();

        // Repel other nodes
        for (int i = 0; i < indices.Length - 1; i++)
        {
            GraphButton node = buttons.ElementAt(i);
            for (int j = i + 1; j < indices.Length; j++)
            {
                GraphButton nextNode = buttons.ElementAt(j);
                Vector3 dif = node.transform.localPosition - nextNode.transform.localPosition;
                float distance = dif.magnitude;
                if (distance > maxForceDistance) continue;

                float repelForce = repelFactor * Mathf.Pow(targetDistance, 2) / Mathf.Pow(distance, 2);
                Vector3 repelDistance = dif.normalized * repelForce;

                node.Force += repelDistance;
                nextNode.Force -= repelDistance;
            }
        }

        // Spring nodeNeighbors
        for (int i = 0; i < indices.Length; i++)
        {
            GraphButton node = buttons.ElementAt(i);
            node.Node.NodeNeighbors?.ForEach(neighbor =>
            {
                Vector3 dif = node.transform.localPosition - nodeGraphButtons.GetValueOrDefault(neighbor).transform.localPosition;
                if (dif.sqrMagnitude == 0) return;
                float springForce = dif.magnitude / targetDistance;

                node.Force -= dif.normalized * springForce;
            });
        }

        // Apply force
        foreach (var node in buttons)
        {
            //node.Force += forceFactor * Mathf.Max(floorHeight - node.TheoreticalPosition.y, 0) * 1000 * Vector3.up;
            node.Force *= scale;
            if (node.Force.sqrMagnitude < equilibriumEpsilon || node.IsHeld)
            {
                node.Force = Vector3.zero;
                continue;
            }
            node.Force.x = Mathf.Clamp(node.Force.x, -maxForce, maxForce);
            node.Force.y = Mathf.Clamp(node.Force.y, -maxForce, maxForce);

            if (!float.IsNaN(node.Force.x) && !float.IsNaN(node.Force.y) && !float.IsNaN(node.Force.z))
                node.transform.localPosition += node.Force * forceFactor;
            else
                node.transform.localPosition += Quaternion.AngleAxis(random.Next(360), Vector3.forward) * Vector3.up * forceFactor;
            node.Force = Vector3.zero;
        }

        // Update lines
        HashSet<Node> closedList = new();
        lineRenderer.points.Clear();
        for (int i = 0; i < buttons.Count - 1; i++)
        {
            GraphButton node = GraphButtons.ElementAt(i);
            closedList.Add(node.Node);
            node.Node.NodeNeighbors?.ForEach(neighbor =>
            {
                if (closedList.Contains(neighbor)) return;
                lineRenderer.points.Add(node.transform.localPosition);
                lineRenderer.points.Add(nodeGraphButtons.GetValueOrDefault(neighbor).transform.localPosition);
            });
        }

        if (isHovered) PreviewInteraction();
        UpdateHighlight();

        lineRenderer.SetAllDirty();
    }

    protected override void OnContextChange()
    {
        // Some rng that is consitent if the config file dosn't change,
        // so the graph looks initially the same
        random = new(Context.Config.name.GetHashCode());

        foreach (GraphButton button in nodeGraphButtons.Values)
        {
            Destroy(button.gameObject);
        }
        //graphButtons.Clear();
        nodeGraphButtons.Clear();

        Context.OnNodeChange.AddListener(() =>
        {
            if (currentActiveButton != null)
                currentActiveButton.GetComponentInChildren<Toggle>().SetIsOnWithoutNotify(false);
            if (Context.currentNode != null)
            {
                currentActiveButton = nodeGraphButtons.GetValueOrDefault(Context.currentNode);
                currentActiveButton.GetComponentInChildren<Toggle>().SetIsOnWithoutNotify(true);
            }
        });

        lineRenderer.points.Clear();

        foreach (Node node in Context.Config.nodes)
        {
            CreateNode(node);
        }

        //graphButtons = buttons;
        //foreach (GraphButton gb in graphButtons)
        //{
        //    nodeGraphButtons.Add(gb.Node, gb);
        //}
        //Recenter();
    }

    private GraphButton CreateNode(Node node)
    {
        GameObject nodeObject = Instantiate(nodeButtonPrefab, transform);
        GraphButton graphButton = nodeObject.GetComponent<GraphButton>();
        Toggle toggle = nodeObject.GetComponentInChildren<Toggle>();
        // We operate in 2d space, but unity wants a Vector3
        Vector3 offset = Quaternion.AngleAxis(random.Next(360), Vector3.forward) * Vector3.up;

        nodeObject.transform.localPosition += offset * targetDistance * .5f;
        graphButton.Node = node;
        toggle.group = GetComponent<ToggleGroup>();
        toggle.onValueChanged.AddListener(value =>
        {
            if (!value) toggle.SetIsOnWithoutNotify(true);
            Context.editor.ExecuteCommand(new ChangeSelectedNode(node));
        });

        //graphButtons.Add(graphButton);
        nodeGraphButtons.Add(node, graphButton);

        return graphButton;
    }

    /// <summary>
    /// Recenters the graph to its avg position,
    /// so it is hopefully in frame
    /// </summary>
    public void Recenter()
    {
        Vector3 avgPos = Vector3.zero;
        Vector2 size = (transform as RectTransform).rect.size;
        Vector2 missingOffset = size * (1 - transform.localScale.x);
        if (GraphButtons != null) avgPos = GraphButtons.Select(node => (node.transform as RectTransform).anchoredPosition).Aggregate(Vector2.zero, (a, b) => a + b) / GraphButtons.Count();
        transform.localPosition = -(Vector3)size / (2 * transform.localScale.x) - avgPos + (Vector3)missingOffset;
    }

    public void PreviewLine(GraphButton node)
    {
        loseEnd = node;
    }
    public void TryConnectNode(GraphButton node)
    {
        GraphButton graphButton = GetClosestButton();
        loseEnd = null;

        if (graphButton != null)
        {
            if (!node.Node.neighbors.Contains(graphButton.Node.uniqueName))
            {
                Context.editor.ExecuteCommand(new CreateEdgeCommand(graphButton.Node, node.Node));
                return;
            }
            if (hoveredButtons > 0) return;
        }

        Node newNode = new();
        MultiCommand multi = new(
            "Create Node with Edge",
            false,
            new CreateNodeCommand(newNode),
            new CreateEdgeCommand(node.Node, newNode)
            );

        Context.editor.ExecuteCommand(multi);
        CreateNode(newNode).transform.localPosition = GetPointerPos();
    }
    public void Hover()
    {
        hoveredButtons++;
    }
    public void Unhover()
    {
        hoveredButtons--;
    }

    public void PrepareNewNode()
    {
        GameObject nodeObject = Instantiate(nodeButtonPrefab, transform);
        nodeObject.GetComponentInChildren<Toggle>().SetIsOnWithoutNotify(true);
        nodeObject.GetComponent<CanvasGroup>().alpha = previewAlpha;
    }
    public void DeleteNode(GraphButton graphButton)
    {
        List<ICommand> commandList = new();
        foreach (Node neighbor in graphButton.Node.NodeNeighbors)
        {
            commandList.Add(new DeleteEdgeCommand(graphButton.Node, neighbor, true));
        }
        if (Context.currentNode != null)
        {
            if (Context.currentNode.Equals(graphButton.Node))
            {
                Node newSelectedNode = Context.Config.nodes.Find(node => !node.Equals(graphButton.Node));
                if (newSelectedNode == null) return;
                commandList.Add(new ChangeSelectedNode(newSelectedNode));
            }
        }
        commandList.Add(new CreateNodeCommand(graphButton.Node, true));

        MultiCommand multi = new
        (
            "Delete Node",
            false,
            commandList.ToArray()
        );
        Context.editor.ExecuteCommand(multi);
    }

    public void HighlightEdgesFromNode(Node origin, params Node[] neighbors)
    {
        highlightedEdges.Clear();
        if (origin == null || neighbors == null || neighbors.Length <= 0)
        {
            edgeHighlight.gameObject.SetActive(false);
            return;
        }
        edgeHighlight.gameObject.SetActive(true);

        GraphButton graphButtonOrigin = nodeGraphButtons[origin];
        foreach (Node neighbor in neighbors)
        {
            highlightedEdges.Add(new(graphButtonOrigin, nodeGraphButtons[neighbor]));
        }
    }
    public void HighlightNode(Node node)
    {

    }

    // The base class BaseEventData is used for convinent setup in the editor
    public void OnPointerDown(BaseEventData eventData)
    {
        startMousePos = Input.mousePosition;
    }
    public void OnDrag(BaseEventData eventData)
    {
        //if (!parentTransform.rect.Contains(Input.mousePosition - parentTransform.position)) return;

        wasDraged = true;
        transform.position += Input.mousePosition - startMousePos;
        startMousePos = Input.mousePosition;
    }
    public void OnPointerClick(BaseEventData eventData)
    {
        PointerEventData pointerEventData = (PointerEventData)eventData;
        if (wasDraged)
        {
            wasDraged = false;
            return;
        }

        if (pointerEventData.button == PointerEventData.InputButton.Middle)
        {
            Recenter();
            return;
        }

        if (hoveredEdge != null && pointerEventData.button == PointerEventData.InputButton.Right)
        {
            Context.editor.ExecuteCommand(new DeleteEdgeCommand(hoveredEdge.Item1, hoveredEdge.Item2));
        }

    }
    public void OnScroll(BaseEventData eventData)
    {
        // TODO: Determin if scrolling is a good idea
        //PointerEventData pointerEventData = eventData as PointerEventData;
        //float delta = pointerEventData.scrollDelta.y * scrollSpeed;
        //transform.localScale += new Vector3(delta, delta, delta);
    }
    public void OnPointerEnter(BaseEventData eventData)
    {
        isHovered = true;
    }
    public void OnPointerExit(BaseEventData eventData)
    {
        isHovered = false;
    }

    private void UpdateHighlight()
    {
        if (highlightedEdges.Count <= 0) return;

        edgeHighlight.points.Clear();
        foreach (var edge in highlightedEdges)
        {
            edgeHighlight.points.Add(edge.Item1.transform.localPosition);
            edgeHighlight.points.Add(edge.Item2.transform.localPosition);
        }

        edgeHighlight.SetAllDirty();
    }
    /// <summary>
    /// Previews a new edge or deletion
    /// </summary>
    private void PreviewInteraction()
    {
        previewLine.points.Clear();

        if (loseEnd != null)
        {
            // Preview new connection
            GraphButton shortestButton = GetClosestButton();
            previewLine.color = newEdgeColor;
            previewLine.points.Add(loseEnd.transform.localPosition);
            
            if (shortestButton != null && shortestButton != loseEnd && !loseEnd.Node.neighbors.Contains(shortestButton.Node.uniqueName))
                previewLine.points.Add(shortestButton.transform.localPosition);
            else
                previewLine.points.Add(GetPointerPos());
        }
        else if (hoveredButtons <= 0)
        {
            // Preview delete line
            previewLine.color = deleteEdgeColor;
            float shortestLineDistance = Mathf.Max(newEdgeSnapDistance / 2, 2); ;
            GraphButton b1 = null, b2 = null;
            hoveredEdge = null;

            foreach (var button in GraphButtons)
            {
                foreach (var n in button.Node.NodeNeighbors)
                {
                    var nighborButton = nodeGraphButtons.GetValueOrDefault(n);
                    float dis = GetDistanceToLine(GetPointerPos(), button.transform.localPosition, nighborButton.transform.localPosition);
                    if (dis < shortestLineDistance)
                    {
                        shortestLineDistance = dis;
                        hoveredEdge = new(button.Node, nighborButton.Node);
                        b1 = button;
                        b2 = nighborButton;
                    }
                }
            }

            if (hoveredEdge != null)
            {
                previewLine.points.Add(b1.transform.localPosition);
                previewLine.points.Add(b2.transform.localPosition);
            }
        }

        previewLine.SetAllDirty();
    }
    /// <returns>The mouse position in the Draw Area</returns>
    private Vector2 GetPointerPos()
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(transform as RectTransform, Input.mousePosition, null, out Vector2 pos);
        return pos;
    }
    /// <returns>The closest GraphButton from the mouse limited to buttons within the radius newEdgeSnapDistance</returns>
    private GraphButton GetClosestButton()
    {
        float shortestDis = newEdgeSnapDistance;
        GraphButton shortestButton = null;

        foreach (GraphButton gButton in GraphButtons)
        {
            float distance = Vector2.Distance(gButton.transform.localPosition, GetPointerPos());
            if (distance < shortestDis)
            {
                shortestButton = gButton;
            }
        }

        return shortestButton;
    }
    // https://stackoverflow.com/questions/849211/shortest-distance-between-a-point-and-a-line-segment
    /// <summary>
    /// Calculates the Distance from pos to a line segment defined by start and end
    /// </summary>
    /// <param name="start">Line start point</param>
    /// <param name="end">Line end point</param>
    private float GetDistanceToLine(Vector2 pos, Vector2 start, Vector2 end)
    {
        // Return minimum distance between line segment vw and point p
        float l2 = (end - start).sqrMagnitude; // i.e. |w-v|^2 -  avoid a sqrt
        if (l2 == 0.0) return Vector2.Distance(pos, start);   // v == w case
                                                // Consider the line extending the segment, parameterized as v + t (w - v).
                                                // We find projection of point p onto the line. 
                                                // It falls where t = [(p-v) . (w-v)] / |w-v|^2
                                                // We clamp t from [0,1] to handle points outside the segment vw.
        float t = Mathf.Max(0, Mathf.Min(1, Vector2.Dot(pos - start, end - start) / l2));
        Vector2 projection = start + t * (end - start);  // Projection falls on the segment
        return Vector2.Distance(pos, projection);
    }
}
