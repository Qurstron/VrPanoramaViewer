using JSONClasses;
using UnityEngine;

public class SceneRoot : WorldSelectable
{
    private Object3D scene;
    public Object3D Scene
    {
        get { return scene; }
        set
        {
            scene = value;
            Subject = value;
            points.Add(new DummyPoint()
            {
                relatedComponent = this
            });
        }
    }

    public Vector3 Position
    {
        set { transform.localPosition = value; }
    }
    public Quaternion Rotation
    {
        set { transform.localRotation = value; }
    }
    public Vector3 Scale
    {
        set { transform.localScale = value; }
    }

    public override void Add()
    {
        GetOrigin().objects.Add(Scene);
    }
    public override void Remove()
    {
        GetOrigin().objects.Remove(scene);
    }

    public override NodeContent GetOrigin()
    {
        return scene.origin;
    }
}
