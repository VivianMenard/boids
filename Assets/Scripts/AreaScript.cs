using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaScript : MonoBehaviour
{
    public float margin;
    [HideInInspector] public Vector3 minPt;
    [HideInInspector] public Vector3 maxPt;

    private Vector3 previousPosition;
    private Vector3 previousScale;

    private void Awake() {
        ComputeBoundaries();
        StoreLocationAndScale();
    }

    void Start() {
        ResetRotation();
    }

    void Update() {}

    private void FixedUpdate() {
        if (HasPositionOrScaleChanged()) {
            ComputeBoundaries();
            StoreLocationAndScale();
        }

        ResetRotation();
    }

    private void ComputeBoundaries() {
        Vector3 delta = transform.localScale / 2;

        minPt = transform.position - delta + margin * Vector3.one;
        maxPt = transform.position + delta - margin * Vector3.one;
    }

    private void ResetRotation() {
        transform.rotation = Quaternion.identity;
    }

    private void StoreLocationAndScale() {
        previousPosition = transform.position;
        previousScale = transform.localScale;
    }

    private bool HasPositionOrScaleChanged() {
        return (
            (transform.position != previousPosition) || 
            (transform.localScale != previousScale)
        );
    }
}
