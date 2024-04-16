using UnityEngine;

public class AreaScript : MonoBehaviour
{
    private Vector3 minPt, maxPt;

    public Vector3 MinPt { get { return minPt; } }
    public Vector3 MaxPt { get { return maxPt; } }

    private void Awake()
    {
        ComputeBoundaries();
    }

    private void ComputeBoundaries()
    {
        Vector3 delta = transform.localScale / 2;

        minPt = transform.position - delta;
        maxPt = transform.position + delta;
    }
}