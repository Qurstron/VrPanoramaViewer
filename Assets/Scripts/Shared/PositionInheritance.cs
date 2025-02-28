using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionInheritance : MonoBehaviour
{
    [SerializeField] private Transform positionalParent;

    void Update()
    {
        transform.position = positionalParent.position;
    }
}
