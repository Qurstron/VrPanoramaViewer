using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static JSONClasses;

public class JSONClasses
{
    // Time points
    [Serializable]
    public class Pic
    {
        public string name;
        public long time;
        public CategoryObject[] categories;
    }
    [Serializable]
    public class CategoryObject
    {
        [Serializable]
        public class Label
        {
            public string header;
            public string content;
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
        public int? categoryparentindex;
        public float? latitudeoffset;
        public string textureoverride;
        public Label[] labels;
        public Line[] lines;

        public void merge(CategoryObject parent)
        {
            if (labels is null) 
                labels = parent.labels;
            else 
                labels = labels.Concat(parent.labels).ToArray();
            if (lines is null) lines = parent.lines;
            else lines = lines.Concat(parent.lines).ToArray();

            if (latitudeoffset is null) latitudeoffset = parent.latitudeoffset;
            if (textureoverride is null) textureoverride = parent.textureoverride;
        }

        public override bool Equals(object obj)
        {
            return obj is CategoryObject @object &&
                   categoryparentindex == @object.categoryparentindex &&
                   latitudeoffset == @object.latitudeoffset &&
                   textureoverride == @object.textureoverride &&
                   EqualityComparer<Label[]>.Default.Equals(labels, @object.labels) &&
                   EqualityComparer<Line[]>.Default.Equals(lines, @object.lines);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(categoryparentindex, latitudeoffset, textureoverride, labels, lines);
        }
    }

    [Serializable]
    public class Node
    {
        public string name;
        public string displayName;
        public string description;
        public string color;

        [NonSerialized]
        public Color convertedColor;
        [NonSerialized]
        public List<Node> neighbors;
        [NonSerialized]
        public GameObject gameObject;
        [NonSerialized]
        public NodeProperties properties;

        public override bool Equals(object obj)
        {
            return obj is Node node &&
                   name == node.name;
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(name);
        }
    }

    [Serializable]
    public class Edge
    {
        public string nodeA;
        public string nodeB;
        public float weight;
    }

    // panorama file (.zip)
    [Serializable]
    public class Config
    {
        [NonSerialized]
        public string name; // not part of the config.json, needs to be set manualy

        public Pic[] pics;
        public string description;
        public long version;
        public string[] categorynames;
        public CategoryObject[] categoryparents;
        public string timeformat;

        private HashSet<string> texNames = new();
        public IEnumerable<string> TextureNames 
        { 
            get 
            {
                if (!isConstructed) throw new Exception("needs to be constructed first");

                HashSet<string> texNames = new();
                foreach (Pic pic in pics)
                {
                    texNames.Add(pic.name);
                    foreach (CategoryObject cat in pic.categories)
                    {
                        texNames.Add(cat.textureoverride);
                    }
                }
                foreach (CategoryObject cat in categoryparents)
                {
                    texNames.Add(cat.textureoverride);
                }

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
            isConstructed = true;

            this.name = name;
            Array.Sort(pics, (a, b) => a.time.CompareTo(b.time)); // just in case

            // merge parent into parent
            for (int i = 0; i < categoryparents.Length; i++)
            {
                List<CategoryObject> visitedParents = new();
                CategoryObject current = categoryparents[i];

                while (current.categoryparentindex is not null)
                {
                    if (current.categoryparentindex.Value < 0 || current.categoryparentindex.Value >= categoryparents.Length) throw new Exception("Out of bounds");
                    CategoryObject next = categoryparents[current.categoryparentindex.Value];
                    // cyclic dependency check
                    if (visitedParents.Contains(next)) throw new Exception("Config is cyclic dependend");
                    visitedParents.Add(next);

                    current.merge(next);
                }
            }
            // merge pic categorys
            for (int j = 0; j < pics.Length; j++)
            {
                if (pics[j].categories == null) continue;

                for (int i = 0; i < pics[j].categories.Length; i++)
                {
                    if (pics[j].categories[i].categoryparentindex is null) continue;
                    pics[j].categories[i].merge(categoryparents[(int)pics[j].categories[i].categoryparentindex]);
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
