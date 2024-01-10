using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaScript : MonoBehaviour
{
    [Range(0, 5)] 
    public float margin;

    [HideInInspector] 
    public Vector3 minPt;
    [HideInInspector] 
    public Vector3 maxPt;

    private Vector3 previousPosition;
    private Vector3 previousScale;
    private float previousMargin;

    private void Awake() {
        ComputeBoundaries();
        StoreProperties();
    }

    void Start() {
        ResetRotation();
    }

    void Update() {}

    private void FixedUpdate() {
        if (HavePropertiesChanged()) {
            ComputeBoundaries();
            StoreProperties();
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

    private void StoreProperties() {
        previousPosition = transform.position;
        previousScale = transform.localScale;
        previousMargin = margin;
    }

    private bool HavePropertiesChanged() {
        return (
            (transform.position != previousPosition) || 
            (transform.localScale != previousScale) ||
            (margin != previousMargin)
        );
    }
}
