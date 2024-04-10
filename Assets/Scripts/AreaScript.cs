using UnityEngine;

public class AreaScript : MonoBehaviour
{
    [HideInInspector]
    public Vector3 minPt, maxPt;

    private void Awake()
    {
        ComputeBoundaries();
    }

    private void FixedUpdate()
    {
        if (transform.hasChanged)
        {
            ComputeBoundaries();
            transform.rotation = Quaternion.identity;
            transform.hasChanged = false;
        }
    }

    private void ComputeBoundaries()
    {
        Vector3 delta = transform.localScale / 2;

        minPt = transform.position - delta;
        maxPt = transform.position + delta;
    }
}