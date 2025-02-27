using UnityEngine;

public class PositionInheritance : MonoBehaviour
{
    [SerializeField] private Transform positionalParent;

    void Update()
    {
        transform.position = positionalParent.position;
    }
}
