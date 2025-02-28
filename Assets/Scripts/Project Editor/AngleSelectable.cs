using JSONClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public abstract class AngleSelectable
{
    public virtual string Name { get; set; } = "Vertex Group";
    protected List<AnglePoint> points = new();

    public List<AnglePoint> GetPoints()
    {
        return points;
    }
    public abstract NodeContent GetOrigin();
}
