using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAt : MonoBehaviour
{
    public Transform toLookAt;
    [Header("Constraints")]
    public bool x = false;
    public bool y = false;
    public bool z = false;

    private void Start()
    {
        if (toLookAt == null) toLookAt = Camera.main.transform;
    }
    void Update()
    {
        if (x || y || z)
        {
            Vector3 rot = transform.rotation.eulerAngles;
            Vector3 rotLook = Quaternion.LookRotation(transform.position - toLookAt.transform.position, Vector3.up).eulerAngles;
            transform.rotation = Quaternion.Euler(x ? rot.x : rotLook.x, y ? rot.y : rotLook.y, z ? rot.z : rotLook.z);
        }
        else
        {
            transform.LookAt(toLookAt);
        }
    }
}
