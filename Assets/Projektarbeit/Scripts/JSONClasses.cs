using GLTFast.Schema;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using UnityEngine;
using static JSONClasses;
using static JSONClasses.NodeContent;

public class JSONClasses
{
    // panorama file (.zip)
    [Serializable]
    public class Config
    {
        [NonSerialized]
        public string name; // not part of the config.json, needs to be set manualy
        [NonSerialized]
        public Node rootNode;

        public Node[] nodes;
        public string description;
        public long version;
        public string[] categoryNames;
        public NodeContent[] categoryParents;
        //public Edge[] edges;
        public string root;

        private readonly List<NodeContent> constructedNodeContents = new();
        private readonly List<NodeContent> visitedNodeContents = new();

        public IEnumerable<string> TextureNames 
        { 
            get
            {
                if (!isConstructed)
                {
                    throw new Exception("Config is not constructed! please call Construct(...) to construct config first");
                }

                HashSet<string> texNames = new();
                foreach (NodeContent nodeContent in AllNodeContents())
                {
                    texNames.Add(nodeContent.texture);
                }

                //foreach (Pic pic in pics)
                //{
                //    texNames.Add(pic.name);
                //    foreach (NodeContent cat in pic.categories)
                //    {
                //        texNames.Add(cat.texture);
                //    }
                //}
                //foreach (NodeContent cat in categoryparents)
                //{
                //    texNames.Add(cat.texture);
                //}

                texNames.Remove(null);
                return texNames;
            }
        }

        private bool isConstructed = false;
        public bool IsConstructed { get { return isConstructed; } }
        // builds categoryparents
        public void Construct(string name)
        {
            if (isConstructed) return;
            this.name = name;

            // merge parents into nodeContent
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].content == null) continue;

                for (int j = 0; j < nodes[i].content.Length; j++)
                {
                    ConstructNodeContent(nodes[i].content[j]);
                }
            }

            Dictionary<string, Node> namedNodes = new();
            foreach (Node node in nodes)
            {
                namedNodes.Add(node.name, node);
            }

            if (namedNodes.TryGetValue(root, out Node r)) rootNode = r;
            else throw new Exception($"The root: {root} could not be found in nodes");
            for (int i = 0; i < nodes.Length; i++)
            {
                Node node = nodes[i];

                if (ColorUtility.TryParseHtmlString(node.color, out Color color)) node.convertedColor = color;
                else throw new Exception($"Could not convert Color: {node.color} to Unity Color object");
                
                node.nodeNeighbors = new();
                foreach (var neighborEdge in node.neighbors)
                {
                    if (namedNodes.TryGetValue(neighborEdge.node, out Node neighbor)) node.nodeNeighbors.Add(new Node.NodeEdge(neighbor, neighborEdge.wieght));
                    else throw new Exception($"The neighbor: {neighborEdge.node} could not be found in nodes");
                }
            }

            isConstructed = true;
        }
        public IEnumerable<NodeContent> AllNodeContents()
        {
            if (!isConstructed)
            {
                throw new Exception("Config is not constructed! please call Construct(...) to construct config first");
            }

            foreach (Node node in nodes)
            {
                if (node.content == null) continue;
                foreach (NodeContent nodeContent in node.content)
                {
                    yield return nodeContent;
                }
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Config config &&
                   name == config.name &&
                   version == config.version;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(name, version);
        }

        private void ConstructNodeContent(NodeContent nodeContent)
        {
            visitedNodeContents.Clear();
            ConstructNodeContentRek(nodeContent);
        }
        private void ConstructNodeContentRek(NodeContent nodeContent)
        {
            if (constructedNodeContents.Contains(nodeContent)) return;
            constructedNodeContents.Add(nodeContent);
            if (nodeContent.categoryParentIndices == null) return;
            if (visitedNodeContents.Contains(nodeContent)) throw new Exception("Cyclic dependency in NodeContent parent detected");
            visitedNodeContents.Add(nodeContent);

            for (int i = 0; i < nodeContent.categoryParentIndices.Length; i++)
            {
                NodeContent parent = categoryParents[nodeContent.categoryParentIndices[i]];
                ConstructNodeContentRek(parent);
                nodeContent.Merge(parent);
            }
        }
    }

    [Serializable]
    public class Node
    {
        public string name; // unique name, only used internaly
        public string displayName; // what the user sees (duplicates allowed)
        public string description;
        public string color;
        public NodeEdgeString[] neighbors; // only contains the neighbors in string format, use nodeNeighbors for objects
        public NodeContent[] content;

        [NonSerialized]
        public Color convertedColor;
        [NonSerialized]
        public GameObject gameObject;
        [NonSerialized]
        public NodeProperties properties;
        [NonSerialized]
        public List<NodeEdge> nodeNeighbors; // contains the references to the actual node objects

        public override bool Equals(object obj)
        {
            return obj is Node node &&
                   name == node.name;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(name);
        }

        [Serializable]
        public class NodeEdgeString
        {
            public string node;
            public float wieght;
        }
        public class NodeEdge
        {
            public Node node;
            public float wieght;

            public NodeEdge(Node node, float wieght)
            {
                this.node = node;
                this.wieght = wieght;
            }
        }
    }

    [Serializable]
    public class NodeContent
    {
        [Serializable]
        public class Label
        {
            public string header;
            public string content;
            public string details;
            public float[] pos;
        }
        [Serializable]
        public class Line
        {
            public string color;
            public float width;
            public bool flipcoords;
            public List<float[]> coords;
        }

        //public string name;
        public int[] categoryParentIndices;
        public float? latitudeOffset;
        public string texture;
        public Label[] labels;
        public Line[] lines;
        public Object3D[] objects;

        public void Merge(NodeContent parent)
        {
            if (labels is null)
                labels = parent.labels;
            else
                labels = labels.Concat(parent.labels).ToArray();
            if (lines is null) lines = parent.lines;
            else lines = lines.Concat(parent.lines).ToArray();

            latitudeOffset ??= parent.latitudeOffset;
            texture ??= parent.texture;
        }

        public override bool Equals(object obj)
        {
            return obj is NodeContent content &&
                   EqualityComparer<int[]>.Default.Equals(categoryParentIndices, content.categoryParentIndices) &&
                   latitudeOffset == content.latitudeOffset &&
                   texture == content.texture &&
                   EqualityComparer<Label[]>.Default.Equals(labels, content.labels) &&
                   EqualityComparer<Line[]>.Default.Equals(lines, content.lines);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(categoryParentIndices, latitudeOffset, texture, labels, lines);
        }
    }

    [Serializable]
    public class Object3D
    {
        public string file;
        public Transform transform;

        [Serializable]
        public class Transform
        {
            public float[] translation;
            public float[] rotation;
            public float[] scale;
        }
    }

    // main menu panorama file
    [Serializable]
    public class PanoramaMenuEntry
    {
        public string name;
        //public string Description { get; set; }
        public long size;
        public long version;
    }
}
