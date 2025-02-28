using JSONClasses;
using UnityEngine;

public abstract class WorldSelectable : AngleSelectable
{
    public MergeSubject Subject { get; protected set; }
    private GameObject go;
    public GameObject gameObject
    {
        get
        {
            if (go == null)
            {
                go = sphereController.GetGameObject(Subject);
            }

            return go;
        }
        set { go = value; }
    }
    public PanoramaSphereController sphereController;
    public Transform transform
    {
        get { return gameObject.transform; }
    }
    public abstract void Remove();
    public abstract void Add();
}
