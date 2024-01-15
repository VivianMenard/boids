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

        Move();
        TeleportIfOutOfBorders();
    }

    protected bool IsAVisibleBoid(Collider collider) {
        return !IsMyCollider(collider) && IsBoidCollider(collider) && IsInMyFOV(collider);
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
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
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
        transform.position = transform.position + parameters.velocity * Direction * Time.deltaTime;
    }

    protected void TeleportIfOutOfBorders() {
        Vector3 newPosition = transform.position;

        if (transform.position.x < area.minPt.x) {
            newPosition.x = area.maxPt.x;
        } else if (transform.position.x > area.maxPt.x) {
            newPosition.x = area.minPt.x;
        }

        if (transform.position.y < area.minPt.y) {
            newPosition.y = area.maxPt.y;
        } else if (transform.position.y > area.maxPt.y) {
            newPosition.y = area.minPt.y;
        }

         if (transform.position.z < area.minPt.z) {
            newPosition.z = area.maxPt.z;
        } else if (transform.position.z > area.maxPt.z) {
            newPosition.z = area.minPt.z;
        }

        transform.position = newPosition;
    }
}
