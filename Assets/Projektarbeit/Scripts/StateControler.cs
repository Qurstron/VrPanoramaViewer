using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateControler : MonoBehaviour
{
    private bool isMenuActive = true;
    private GameObject[] menuObjects = null;
    private GameObject[] viewerObjects = null;

    void Start()
    {
        menuObjects = GameObject.FindGameObjectsWithTag("Menu");
        viewerObjects = GameObject.FindGameObjectsWithTag("Viewer");

        foreach (var gameObject in viewerObjects)
        {
            gameObject.SetActive(false);
        }
    }

    public void ToggleMenu()
    {
        isMenuActive = !isMenuActive;

        foreach (var gameObject in viewerObjects)
        {
            gameObject.SetActive(!isMenuActive);
        }
        foreach (var gameObject in menuObjects)
        {
            gameObject.SetActive(isMenuActive);
        }
    }
}
