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
        [NonSerialized]
        public string folderPath;

        public Node[] nodes;
        public string description;
        public long version;
        public string[] categoryNames;
        public NodeContent[] categoryParents;
        //public Edge[] edges;
        public string root;
        public bool isWip;

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

                texNames.Remove(null);
                return texNames;
            }
        }
        public IEnumerable<string> ObjectsPaths
        {
            get
            {
                if (!isConstructed)
                {
                    throw new Exception("Config is not constructed! please call Construct(...) to construct config first");
                }

                HashSet<string> objPaths = new();
                foreach (NodeContent nodeContent in AllNodeContents())
                {
                    if (nodeContent.objects == null) continue;
                    foreach (Object3D obj in nodeContent.objects)
                    {
                        objPaths.Add(obj.file);
                    }
                }

                objPaths.Remove(null);
                return objPaths;
            }
        }

        private bool isConstructed = false;
        public bool IsConstructed { get { return isConstructed; } }
        // preprocess config so that is it easyer to work with
        public void Construct(string name)
        {
            if (isConstructed) return;
            this.name = name;

            if (isWip) Debug.LogWarning("Config is flaged as WIP");

            // validate some basic node characteristic
            HashSet<string> nodeNames = new();
            foreach (Node node in nodes)
            {
                if (nodeNames.Contains(node.name)) throw new Exception($"Node name: {node.name} is not unique");
                nodeNames.Add(node.name);

                foreach (var edge in node.neighbors)
                {
                    if (edge.weight <= 0) throw new Exception($"Node: {node.name} cannot have a negative or zero wieght to neighbor: {edge.node}");
                }
            }

            // merge parents into nodeContent
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].content == null) continue;

                for (int j = 0; j < nodes[i].content.Length; j++)
                {
                    ConstructNodeContent(nodes[i].content[j]);
                }
            }

            // convert node data
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
                    if (namedNodes.TryGetValue(neighborEdge.node, out Node neighbor)) node.nodeNeighbors.Add(new Node.NodeEdge(neighbor, neighborEdge.weight));
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

            if (nodeContent.categoryParentIndices == null)
            {
                nodeContent.Construct();
                return;
            }

            if (visitedNodeContents.Contains(nodeContent)) throw new Exception("Cyclic dependency in NodeContent parent detected");
            visitedNodeContents.Add(nodeContent);

            for (int i = 0; i < nodeContent.categoryParentIndices.Length; i++)
            {
                NodeContent parent = categoryParents[nodeContent.categoryParentIndices[i]];
                ConstructNodeContentRek(parent);
                nodeContent.Merge(parent);
            }

            nodeContent.Construct();
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
            public float weight;
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
        public void Construct()
        {
            if (objects == null) return;
            foreach (Object3D obj in objects)
            {
                if (obj.addOns == null) continue;
                foreach (AddOn addOn in obj.addOns)
                {
                    AddOn.Type type;
                    if (!Enum.TryParse(addOn.type, out type)) throw new Exception($"invalid type: {addOn.type}");
                    addOn.typeClass = type;
                }
            }
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
        public AddOn[] addOns;

        [Serializable]
        public class Transform
        {
            public float[] translation;
            public float[] rotation;
            public float[] scale;

            public Vector3 Translation
            {
                get
                {
                    if (translation == null) return Vector3.zero;
                    return new(translation[0], translation[1], translation[2]);
                }
            }
            public Quaternion Rotation
            {
                get
                {
                    if (rotation == null) return Quaternion.identity;
                    return Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
                }
            }
            public Vector3 Scale
            {
                get
                {
                    if (scale == null) return Vector3.one;
                    return new(scale[0], scale[1], scale[2]);
                }
            }

            //public override void Validate()
            //{
            //    if (translation != null)
            //    {
            //        if (translation.Length != 3) Invalidate("translation is not of size 3");
            //    }
            //    if (rotation != null)
            //    {
            //        if (rotation.Length != 3) Invalidate("rotation is not of size 3");
            //    }
            //    if (scale != null)
            //    {
            //        if (scale.Length != 3) Invalidate("scale is not of size 3");
            //    }
            //}
        }
    }

    [Serializable]
    public class AddOn
    {
        public string path;
        public string type;
        [NonSerialized]
        public Type typeClass;

        public enum Type
        {
            Label
        }

        [Serializable]
        public class Label
        {
            public string content;
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

    //public abstract class Validatable
    //{
    //    public bool IsValid { get; private set; } = true;
    //    protected void Invalidate(string reason)
    //    {
    //        Debug.LogError(reason);
    //        IsValid = false;
    //    }
    //    public abstract void Validate();
    //}
}
