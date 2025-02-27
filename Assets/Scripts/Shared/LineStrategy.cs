using UnityEngine;

public abstract class LineStrategy
{
    public static readonly LineStrategy shortestPath = new ShortestPath();
    public static readonly LineStrategy linearPath = new LinearPath();

    public abstract Vector3 Interpolate(Vector2 start, Vector2 end, float t);
}

public class ShortestPath : LineStrategy
{
    public override Vector3 Interpolate(Vector2 start, Vector2 end, float t)
    {
        return Vector3.Slerp(PanoramaSphereController.convertArrayToPos(start), PanoramaSphereController.convertArrayToPos(end), t);
    }
}
public class LinearPath : LineStrategy
{
    public override Vector3 Interpolate(Vector2 start, Vector2 end, float t)
    {
        return PanoramaSphereController.convertArrayToPos(Vector2.Lerp(start, end, t));
    }
}
