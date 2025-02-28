using JSONClasses;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LineComponent : WorldSelectable
{
    public override string Name { get => line.name; set => line.name = value; }
    public class LineAnglePoint : AnglePoint
    {
        public LineComponent lineComponent;
        public int index;

        public override Vector2 Angle
        {
            get { return lineComponent.GetCoord(index); }
            set { lineComponent.SetCoord(index, value); }
        }
        public override Vector2 JsonAngle
        {
            get
            {
                return new(lineComponent.Line.coords[index][0], lineComponent.Line.coords[index][1]);
            }
            set
            {
                lineComponent.Line.coords[index][0] = value.x;
                lineComponent.Line.coords[index][1] = value.y;
                lineComponent.Coords = lineComponent.Line.coords;
            }
        }

        public LineAnglePoint(LineComponent lineComponent, int index)
        {
            this.lineComponent = lineComponent;
            this.index = index;
            relatedComponent = lineComponent;
        }
    }

    private Line line;
    public Line Line
    {
        get { return line; }
        set
        {
            line = value;
            Subject = value;

            Coords = line.coords;
            Width = line.width;
            Color = line.color;

            for (int i = 0; i < Coords.Count; i++)
            {
                points.Add(new LineAnglePoint(this, i));
            }
        }
    }
    private LineRenderer LineRenderer
    {
        get { return gameObject.GetComponentInChildren<LineRenderer>();}
    }
    private List<float[]> coords;

    public string JsonColor
    {
        get { return line.color; }
        set
        {
            line.color = value;
            Color = value;
        }
    }
    public float JsonWidth
    {
        get { return line.width; }
        set
        {
            line.width = value;
            Width = value;
        }
    }

    public List<float[]> Coords
    {
        get { return coords; }
        set
        {

            List<float[]> copy = new(value.Count);
            value.ForEach((item) =>
            {
                copy.Add((float[])item.Clone());
            });
            coords = copy;

            RebuildLine();
        }
    }
    public string Color
    {
        set
        {
            Gradient g = new();
            g.SetKeys(new GradientColorKey[] { new(QUtils.StringToColor(value), 0) }, new GradientAlphaKey[] { new(1, 0) });
            LineRenderer.colorGradient = g;
        }
    }
    public float Width
    {
        set
        {
            LineRenderer.widthCurve = new AnimationCurve(new Keyframe[] { new(0, value * sphereController.objectScale * sphereController.Radius * 10) });
        }
    }

    private void RebuildLine(List<float[]> coords = null)
    {
        coords ??= Coords;

        var points = coords.Select(coord => line.flipcoords ? new Vector2(coord[1], coord[0]) : new Vector2(coord[0], coord[1])).ToArray();
        List<Vector3> positions = sphereController.CalcPoints(points, LineStrategy.shortestPath);
        LineRenderer.positionCount = positions.Count;
        LineRenderer.SetPositions(positions.ToArray());
    }

    public Vector2 GetCoord(int index)
    {
        float[] coord = Coords[index];
        return new(coord[0], coord[1]);
    }
    public void SetCoord(int index, Vector2 coord)
    {
        //List<float[]> copy = new(Coords.Count);
        //Coords.ForEach((item) =>
        //{
        //    copy.Add((float[])item.Clone());
        //});
        Coords[index] = new float[] { coord.x, coord.y };
        RebuildLine();
    }
    public void AddCoord(Vector2 coord)
    {
        Coords.Add(new float[] { coord.x, coord.y });
        points.Add(new LineAnglePoint(this, Coords.Count - 1));
        line.coords.Add(new float[] { coord.x, coord.y });
    }
    public void RemoveCoord(int index)
    {
        Coords.RemoveAt(index);
        points.RemoveAt(index);

        for (int i = index; i < points.Count; i++)
        {
            (points[i] as LineAnglePoint).index = i;
        }

        RebuildLine();
    }

    public override void Remove()
    {
        GetOrigin().lines.Remove(Line);
    }
    public override void Add()
    {
        GetOrigin().lines.Add(Line);
    }

    public override NodeContent GetOrigin()
    {
        return Line.origin;
    }
}
