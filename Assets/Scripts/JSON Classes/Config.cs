using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


namespace JSONClasses
{
    /// <summary>
    /// Contains the entire configfile
    /// </summary>
    [Serializable]
    public class Config : Validatable
    {
        [NonSerialized]
        [JsonIgnore] public string path;
        [NonSerialized]
        [JsonIgnore] public Node rootNode;
        [NonSerialized]
        [JsonIgnore] public Dictionary<string, byte[]> contentData = new();
        [NonSerialized]
        [JsonIgnore] public UnityEvent<HotReloadType> OnHotReload = new();

        [JsonProperty(Required = Required.Always)] public string author;
        [JsonProperty(Required = Required.Always)] public string root;
        public long version = 0;
        public List<Node> nodes = new();
        public string description;
        public List<string> categoryNames = new();
        public List<NodeContent> categoryParents = new();
        public bool isWip;

        private readonly List<NodeContent> constructedNodeContents = new();
        private readonly HashSet<NodeContent> visitedNodeContents = new();

        private bool isConstructed = false;
        [JsonIgnore] public bool IsConstructed { get { return isConstructed; } }

        [JsonIgnore] public IEnumerable<string> TextureNames
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
                if (categoryParents != null)
                {
                    foreach (NodeContent nodeContent in categoryParents)
                    {
                        texNames.Add(nodeContent.texture);
                    }
                }

                texNames.Remove(null);
                return texNames;
            }
        }
        [JsonIgnore] public IEnumerable<string> ObjectsPaths
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

        public Config(bool isConstructed = false)
        {
            this.isConstructed = isConstructed;
        }

        /// <summary>
        /// Preprocess config so that is it easier to work with
        /// </summary>
        /// <returns>True if no problems arise</returns>
        /// <exception cref="Exception"></exception>
        public bool Construct()
        {
            if (isConstructed) return !HasProblems;
            if (isWip) Debug.LogWarning("Config is flaged as WIP");

            problems.Clear();

            // validate some basic node characteristic
            HashSet<string> nodeNames = new();
            foreach (Node node in nodes)
            {
                if (nodeNames.Contains(node.uniqueName)) AddProblem($"Node name: {node.uniqueName} is not unique"); //throw new Exception($"Node name: {node.uniqueName} is not unique");
                nodeNames.Add(node.uniqueName);

                if (node.neighbors == null) continue; // node is isolated
            }

            // convert node data
            Dictionary<string, Node> namedNodes = new();
            foreach (Node node in nodes)
            {
                namedNodes.Add(node.uniqueName, node);
                node.config = this;
            }
            if (namedNodes.TryGetValue(root, out Node r)) rootNode = r;
            else AddProblem($"The root: {root} could not be found in nodes");
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];

                if (node.neighbors == null) continue;
                foreach (var neighborEdge in node.neighbors)
                {
                    if (!namedNodes.TryGetValue(neighborEdge, out Node neighbor))
                        AddProblem($"The neighbor: {neighborEdge} from node: {node} could not be found in nodes");
                }
            }

            constructedNodeContents.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                Node node = nodes[i];
                if (node.content == null) continue;

                for (int j = 0; j < node.content.Count; j++)
                {
                    NodeContent content = node.content[j];

                    CreateNodeContentParents(node, content, j);
                    visitedNodeContents.Clear();
                    //ConstructNodeContentRek(content);
                }
            }

            Validate(this, true);
            isConstructed = true;
            return !HasProblems;
        }
        public IEnumerable<NodeContent> AllNodeContents()
        {
            if (!isConstructed)
            {
                throw new Exception("Config is not constructed! please call Construct(...) to construct config first");
            }

            if (categoryParents != null)
            {
                foreach (NodeContent nc in categoryParents)
                {
                    yield return nc;
                }
            }
            if (nodes != null)
            {
                foreach (Node node in nodes)
                {
                    if (node.content == null) continue;
                    foreach (NodeContent nodeContent in node.content)
                    {
                        yield return nodeContent;
                    }
                }
            }
        }
        public Node GetNodeByName(string name)
        {
            foreach (Node node in nodes)
            {
                if (node.uniqueName.Equals(name)) return node;
            }

            return null;
        }
        public string GenerateUniqueName()
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "").Replace("+", "");
            return GuidString;
        }

        public override bool Equals(object obj)
        {
            return obj is Config config &&
                   path == config.path &&
                   version == config.version;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(path, version);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nodeContent"></param>
        /// <returns>True if no cyclic dependency is detected</returns>
        /// <exception cref="Exception"></exception>
        private bool ConstructNodeContentRek(NodeContent nodeContent)
        {
            if (constructedNodeContents.Contains(nodeContent)) return true;
            constructedNodeContents.Add(nodeContent);
            if (nodeContent.categoryParentIndices == null)
                return true;

            if (visitedNodeContents.Contains(nodeContent)) return false; // throw new Exception("Cyclic dependency in NodeContent parent detected");
            visitedNodeContents.Add(nodeContent);

            bool isCyclicFree = true;
            for (int i = 0; i < nodeContent.categoryParentIndices.Count; i++)
            {
                NodeContent parent = categoryParents[nodeContent.categoryParentIndices[i]];
                isCyclicFree = isCyclicFree && ConstructNodeContentRek(parent);
            }
            return isCyclicFree;
        }
        private void CreateNodeContentParents(Node node, NodeContent content, int j)
        {
            if (content.categoryParentIndices == null) return;

            foreach (int index in content.categoryParentIndices)
            {
                if (index < 0)
                {
                    int nodeIndex = -index - 1;
                    if (nodeIndex >= node.NodeNeighbors.Count)
                    {
                        AddProblem($"Neighbor NodeContent index out of bounds in Node {node}");
                        continue;
                    }
                    else if (j >= node.NodeNeighbors[nodeIndex].content.Count)
                    {
                        AddProblem($"Neighbor NodeContent related content index out of bounds in Node {node}");
                        continue;
                    }
                    //content.Parents.Add(node.NodeNeighbors[nodeIndex].content[j]);
                }
                else
                {
                    if (index >= categoryParents.Count)
                    {
                        AddProblem($"CategoryParents NodeContent index out of bounds in Node {node}");
                        continue;
                    }
                    //content.Parents.Add(categoryParents[index]);
                }
            }
        }

        protected override void OnValidate()
        {
            categoryNames ??= new();
            if (categoryNames.Count <= 0) categoryNames.Insert(0, "Main");

            foreach (Node node in nodes)
            {
                if (node.content != null)
                {
                    if (node.content.Count > categoryNames.Count)
                    {
                        AddProblem($"More node contents in {node.uniqueName} than category names");
                    }
                    int i = 0;
                    foreach(NodeContent content in node.content)
                    {
                        if (i >= categoryNames.Count) break;
                        if (string.IsNullOrEmpty(content.name))
                        {
                            content.name = categoryNames[i];
                        }
                        i++;
                    }
                }
                Validate(node);
            }

            if (HasProblems)
            {
                Debug.LogError($"Problems found in Validating {name}");
                foreach (string problem in problems)
                {
                    Debug.Log(problem);
                }
            }
        }

        public enum HotReloadType
        {
            Config,
            File
        }
    }
}
