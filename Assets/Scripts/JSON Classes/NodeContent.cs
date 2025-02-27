using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using static QUtils;

namespace JSONClasses
{
    [Serializable]
    public class NodeContent : Validatable
    {
        public List<int> categoryParentIndices = new();
        public float? latitudeOffset;
        public string texture;
        public List<Label> labels = new();
        public List<Line> lines = new();
        public List<Object3D> objects = new();
        public List<string> excludes = new();
        [JsonIgnore] public Node node;
        [JsonIgnore] public int indexInNode;

        [JsonIgnore] public NodeContent[] Parents
        {
            get
            {
                NodeContent[] parents = new NodeContent[categoryParentIndices.Count];
                int i = 0;

                foreach (int index in categoryParentIndices)
                {
                    if (index < 0)
                    {
                        int nodeIndex = -index - 1;
                        var content = node.NodeNeighbors[nodeIndex].content;
                        while (indexInNode >= content.Count)
                        {
                            content.Add(new());
                        }
                        parents[i] = content[indexInNode];
                    }
                    else
                    {
                        parents[i] = node.config.categoryParents[index];
                    }

                    i++;
                }

                return parents;
            }
        }
        /// <summary>
        /// Used to compare NodeContents created by GetFullObj()
        /// </summary>
        [NonSerialized]
        [JsonIgnore] private NodeContent ogReferenze;

        /// <summary>
        /// Gets a copy with all of its parents merge into it
        /// </summary>
        public NodeContent GetFullObj()
        {
            NodeContent clone = DeepClone();

            foreach (MergeSubject subject in AllWorldObjects())
            {
                subject.origin = this;
            }

            clone.lines = new();
            clone.lines.AddRange(lines);
            clone.labels = new();
            clone.labels.AddRange(labels);
            clone.objects = new();
            clone.objects.AddRange(objects);

            foreach (NodeContent parent in Parents)
            {
                clone.Merge(parent.GetFullObj());
            }

            return clone;
        }
        private void Merge(NodeContent parent)
        {
            if (labels is null) labels = parent.labels;
            else labels.AddRange(parent.labels);
            if (lines is null) lines = parent.lines;
            else lines.AddRange(parent.lines);
            if (objects is null) objects = parent.objects;
            else objects.AddRange(parent.objects);

            if (excludes != null)
            {
                foreach (string exclude in excludes)
                {
                    labels.RemoveAll(obj => obj.unquieID == exclude);
                    lines.RemoveAll(obj => obj.unquieID == exclude);
                    objects.RemoveAll(obj => obj.unquieID == exclude);

                    //foreach (var obj in objects)
                    //{
                    //    obj.addOns.RemoveAll(addOn => addOn.unquieID == exclude);
                    //}
                }
            }

            latitudeOffset ??= parent.latitudeOffset;
            texture ??= parent.texture;
        }
        // https://stackoverflow.com/questions/129389/how-do-you-do-a-deep-copy-of-an-object-in-net
        private NodeContent DeepClone()
        {
            NodeContent clone;
            using var ms = new MemoryStream();
            var formatter = new BinaryFormatter();

            formatter.Serialize(ms, this);
            ms.Position = 0;

            clone = formatter.Deserialize(ms) as NodeContent;
            clone.ogReferenze = ogReferenze;
            clone.labels ??= new();
            clone.lines ??= new();
            clone.objects ??= new();

            return clone;
        }

        public IEnumerable<MergeSubject> AllWorldObjects()
        {
            foreach (var obj in labels)
            {
                yield return obj;
            }
            foreach (var obj in lines)
            {
                yield return obj;
            }
            foreach (var obj in objects)
            {
                yield return obj;
            }
        }

        protected override void OnValidate()
        {
            ogReferenze = this;

            labels ??= new();
            lines ??= new();
            objects ??= new();

            foreach (MergeSubject subject in AllWorldObjects())
            {
                subject.origin = this;
                Validate(subject);
            }
        }

        public override bool Equals(object obj)
        {
            return obj is NodeContent content &&
                   (ogReferenze == content.ogReferenze);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ogReferenze);
        }
    }

    [Serializable]
    public class PanoramaTexture : MergeSubject
    {
        public float? latitudeOffset;
        public string texture;

        protected override void OnValidate()
        {

        }
    }
    [Serializable]
    public class Label : MergeSubject
    {
        public string header = "";
        public string content = "";
        public string details;
        public float[] pos;

        protected override void OnValidate()
        {
            if (pos.Length != 2)
            {
                AddProblem("pos is not of size 2");
            }
        }
    }
    [Serializable]
    public class Line : MergeSubject
    {
        public string color;
        public float width;
        public bool flipcoords;
        public List<float[]> coords;
        [JsonIgnore] public Color Color
        {
            get { return StringToColor(color); }
        }

        protected override void OnValidate()
        {
            foreach (var coord in coords)
            {
                if (coord.Length != 2)
                {
                    AddProblem("at least 1 coord in coords is not of size 2");
                    break;
                }
            }

            ValidateColor(ref color);
        }
    }
    [Serializable]
    public class Object3D : MergeSubject
    {
        public string file;
        public JSONTransform transform = new();
        public List<AddOn> addOns = new();

        protected override void OnValidate()
        {
            Validate(transform);

            addOns ??= new();
            foreach (AddOn addOn in addOns)
            {
                Validate(addOn);
            }
        }

    }
    [Serializable]
    public class AddOn : MergeSubject
    {
        public string path;
        public string label;
        public string outlineColor;
        public float? outlineWidth;
        public bool isReflectionDynamic;
        public JSONTransform transform;
        public OutlineType? outlineType;
        [JsonIgnore] public Object3D object3D;

        [JsonIgnore] public Color OutlineColor
        {
            get { return StringToColor(outlineColor); }
        }

        protected override void OnValidate()
        {
            transform ??= new();
            Validate(transform);
            if (outlineWidth != null)
                ValidateColor(ref outlineColor);
        }
        public override void PrepareSave()
        {
            base.PrepareSave();
            if (outlineWidth <= 0) outlineWidth = null;
            if (transform == new JSONTransform()) transform = null;
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum OutlineType
        {
            All,
            Visible,
            Hidden
        }
    }
    [Serializable]
    public class JSONTransform : Validatable
    {
        public float[] translation;
        public float[] rotation;
        public float[] scale;

        [JsonIgnore] public Vector3 Translation
        {
            get
            {
                if (translation == null) return Vector3.zero;
                return new(translation[0], translation[1], translation[2]);
            }
        }
        [JsonIgnore] public Quaternion Rotation
        {
            get
            {
                if (rotation == null) return Quaternion.identity;
                return Quaternion.Euler(rotation[0], rotation[1], rotation[2]);
            }
        }
        [JsonIgnore] public Vector3 Scale
        {
            get
            {
                if (scale == null) return Vector3.one;
                return new(scale[0], scale[1], scale[2]);
            }
        }

        protected override void OnValidate()
        {
            if (translation != null)
            {
                if (translation.Length != 3) AddProblem("translation is not of size 3");
            }
            if (rotation != null)
            {
                if (rotation.Length != 3) AddProblem("rotation is not of size 3");
            }
            if (scale != null)
            {
                if (scale.Length != 3) AddProblem("scale is not of size 3");
            }
        }
    }

    /// <summary>
    /// Used to differentiate object NodeContent parents
    /// </summary>
    [Serializable]
    public abstract class MergeSubject : Validatable
    {
        [JsonIgnore] public NodeContent origin;
        public string unquieID = Guid.NewGuid().ToString();

        public override void PrepareSave()
        {
            base.PrepareSave();
            origin = null;
        }
    }
}
