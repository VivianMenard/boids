using UnityEngine;

public class AreaScript : MonoBehaviour
{
    [HideInInspector] 
    public Vector3 minPt;
    [HideInInspector] 
    public Vector3 maxPt;
    
    [SerializeField, Range(0, 5)] 
    private float margin;
    private float previousMargin;

    private void Awake() {
        ComputeBoundaries();
    }

    void Start() {
        previousMargin = margin;
    }

    private void FixedUpdate() {
        if (transform.hasChanged || previousMargin != margin) {
            ComputeBoundaries();
            previousMargin = margin;
            transform.rotation = Quaternion.identity;
            transform.hasChanged = false;
        }
    }

    private void ComputeBoundaries() {
        Vector3 delta = transform.localScale / 2;

        minPt = transform.position - delta + margin * Vector3.one;
        maxPt = transform.position + delta - margin * Vector3.one;
    }
}