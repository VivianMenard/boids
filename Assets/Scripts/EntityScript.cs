using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class EntityScript : MonoBehaviour
{
    private static int nextId=0;

    public Vector3 Direction;

    protected int id;

    protected AreaScript area;
    protected EntitiesManagerScript entitiesManager;

    protected EntityParameters parameters;

    protected int visionDistance;
    protected float velocityBonusFactor=1;
    protected bool velocityBonusActivated=false;

    protected Quaternion lastRotation;
    protected Quaternion targetRotation;
    protected int sinceLastCalculation;

    void Start() {
        id = nextId++;

        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();
        entitiesManager = GameObject.FindGameObjectWithTag("BoidsManager").
            GetComponent<EntitiesManagerScript>();

        InitParams();

        visionDistance = parameters.visionDistance;

        SetDirection(GetRandomDirection(), initialization:true);
    }

    protected abstract void InitParams();

    protected abstract void ComputeNewDirection();

    protected void FixedUpdate() {
        if (entitiesManager.clock == id % entitiesManager.calculationInterval)
            ComputeNewDirection();

        UpdateRotation();
        AdaptVelocity();

        Move();
        TeleportIfOutOfBorders();
    }

    protected bool IsMyCollider(Collider collider) {
        return collider == this.GetComponent<Collider>();
    }

    protected bool IsBoidCollider(Collider collider) {
        return collider.gameObject.layer == LayerMask.NameToLayer("Boids");
    }

    protected bool IsPredatorCollider(Collider collider) {
        return collider.gameObject.layer == LayerMask.NameToLayer("Predators");
    }

    protected bool IsInMyFOV(Collider collider) {
        float angle = Vector3.Angle(
            (collider.transform.position - transform.position).normalized,
            Direction
        );

        return angle <= parameters.visionSemiAngle;
    }

    protected Vector3 GetRandomDirection() {
        return new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;
    }

    protected void SetDirection(Vector3 newDirection, bool initialization = false) {
        Direction = newDirection;

        Quaternion newRotation = Quaternion.LookRotation(Direction);
        
        lastRotation = (initialization) ? newRotation: targetRotation;
        targetRotation = newRotation;

        sinceLastCalculation = 0;
    }

    protected void UpdateRotation() {
        float rotationProgress = (float)sinceLastCalculation / (float)entitiesManager.calculationInterval;
        transform.rotation = Quaternion.Lerp(lastRotation, targetRotation, rotationProgress);

        sinceLastCalculation++;
    }

    protected void Move() {
        transform.position = transform.position + parameters.velocity * velocityBonusFactor * Direction * Time.deltaTime;
    }

    private float ComputePositionAfterTP1D(float position, float min, float max) {
        if (position < min)
            return max;
        if (position > max) 
            return min;

        return position;
    }

    protected Vector3 GetDirectionToPosition(Vector3 position) {
        return (position - transform.position).normalized;
    }

    protected void TeleportIfOutOfBorders() {
        transform.position = new Vector3(
            ComputePositionAfterTP1D(transform.position.x, area.minPt.x, area.maxPt.x),
            ComputePositionAfterTP1D(transform.position.y, area.minPt.y, area.maxPt.y),
            ComputePositionAfterTP1D(transform.position.z, area.minPt.z, area.maxPt.z)
        );
    }

    protected Collider[] GetNearbyColliders() {
        return Physics.OverlapSphere(
            transform.position, 
            visionDistance
        );
    }

    private void AdaptVelocity() {
        if (velocityBonusActivated)
            velocityBonusFactor = Math.Min(
                1 + parameters.maxBonusVelocity, 
                velocityBonusFactor + parameters.velocityIncrement
            );
        else
            velocityBonusFactor = Math.Max(
                1, velocityBonusFactor - parameters.velocityDecrement);
    }
}
