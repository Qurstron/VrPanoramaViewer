using UnityEngine;

public class Cornerer : MonoBehaviour
{
    public RectTransform rect;
    public void Corner()
    {
        //Debug.Log(rect.rect.width);
        rect.localPosition = new Vector3(rect.rect.width / 2, -rect.rect.height / 2, 0);
        //transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
    // ugly
    private void Update()
    {
        Corner();
    }
}
