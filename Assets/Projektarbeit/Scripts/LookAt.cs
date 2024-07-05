using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform toLookAt;

    private void Start()
    {
        if (toLookAt == null) toLookAt = Camera.main.transform;
    }
    void Update()
    {
        transform.LookAt(toLookAt);
    }
}
