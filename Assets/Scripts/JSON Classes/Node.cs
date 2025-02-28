using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using static QUtils;

namespace JSONClasses
{
    [Serializable]
    public class Node : Validatable
    {
        public string uniqueName; // what the user sees (duplicates allowed)
        public string description;
        public string color = "#FFFFFF";
        public List<string> neighbors = new(); // only contains the neighbors in string format, use nodeNeighbors for objects
        public List<NodeContent> content = new();

        [JsonIgnore]
        [NonSerialized]
        public Config config;

        [NonSerialized] public GameObject gameObject;
        //[NonSerialized] public GraphButton graphButton;
        // [NonSerialized] public List<Node> NodeNeighbors = new(); // contains the references to the actual node objects
        [JsonIgnore] public Color Color
        {
            get { return StringToColor(color); }
        }
        [JsonIgnore] public List<Node> NodeNeighbors
        {
            get
            {
                List<Node> nodeNeighbors = new();
                foreach (var n in neighbors)
                {
                    nodeNeighbors.Add(config.GetNodeByName(n));
                }
                return nodeNeighbors;
            }
        }

        /// <summary>
        /// Mimics the Unity HDR color picker tool
        /// </summary>
        /// <param name="materialInstesity">The brightness of the color</param>
        /// <returns>The HDR color to be used in a HDR material</returns>
        public Color GetHDRCorrectedColor(float materialInstesity)
        {
            //float intesityFactor = Mathf.Pow(2, materialInstesity);

            //for (int i = 0; i < config.nodes.Count; i++)
            //{
            //    config.nodes[i].convertedColor *= intesityFactor;
            //    config.nodes[i].convertedColor = config.nodes[i].convertedColor.gamma;
            //}

            return (Color * Mathf.Pow(2, materialInstesity)).gamma;
        }

        public override bool Equals(object obj)
        {
            return obj is Node node &&
                   uniqueName == node.uniqueName;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(uniqueName);
        }

        protected override void OnValidate()
        {
            if (ValidateNotNullorEmpty(color))
            {
                ValidateColor(ref color);
                //color = FormatHexColor(color);
                //Color = StringToColor(color);
            }
            if (string.IsNullOrEmpty(name))
            {
                name = Guid.NewGuid().ToString();
            }

            content ??= new();

            int i = 0;
            foreach (NodeContent nc in content)
            {
                nc.node = this;
                nc.indexInNode = i;
                Validate(nc);
                i++;
            }
        }
    }
}
