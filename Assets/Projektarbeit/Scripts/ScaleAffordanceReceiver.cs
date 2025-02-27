using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AffordanceSystem.Receiver.Primitives;

public class ScaleAffordanceReceiver : FloatAffordanceReceiver
{
    public Transform transformTarget;
    public Vector3 factors = Vector3.one;

    // Start is called before the first frame update
    private new void Start()
    {
        base.Start();
        if (transformTarget == null) transformTarget = transform;

        valueUpdated.AddListener(value =>
        {
            if (value != 0)
                transformTarget.localScale = factors * value;
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
