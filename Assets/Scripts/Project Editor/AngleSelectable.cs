using JSONClasses;
using System.Collections.Generic;

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
