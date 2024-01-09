using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidScript : MonoBehaviour
{
    public int id;
    public Vector3 Direction;
    private AreaScript area;
    private BoidsManagerScript boidsManager;
    private int visionDistance;
    void Start() {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();
        boidsManager = GameObject.FindGameObjectWithTag("BoidsManager").
            GetComponent<BoidsManagerScript>();

        visionDistance = boidsManager.maxVisionDistance;

        SetDirection(GetRandomDirection());
    }
    void Update() {}

    private void FixedUpdate() {
        if (boidsManager.clock == id % boidsManager.nbFrameBetweenUpdates) {
            ComputeNewDirection();
        }
        
        Move();
        TeleportIfOutOfBorders(); 
    }

    private void ComputeNewDirection() {
        Collider[] collidersNearby = Physics.OverlapSphere(
            transform.position, 
            visionDistance
        );

        Vector3 nearbyBoidsDirectionSum = Vector3.zero;
        int nbBoidsInFieldOfView = 0;

        foreach (Collider collider in collidersNearby) {
            if (!IsAVisibleBoid(collider)){
                continue;
            }

            nbBoidsInFieldOfView += 1;

            BoidScript boidScript = collider.GetComponent<BoidScript>();
            nearbyBoidsDirectionSum += boidScript.Direction;
        }

        AdaptVisionDistance(nbBoidsInFieldOfView);

        if (nbBoidsInFieldOfView == 0){
            return;
        }

        Vector3 averageDirection = nearbyBoidsDirectionSum / (float)nbBoidsInFieldOfView;

        Vector3 newDirection = (
            (boidsManager.momentumStrengh * Direction + boidsManager.alignmentStrengh * averageDirection)
            / (boidsManager.momentumStrengh + boidsManager.alignmentStrengh)
        ).normalized;

        SetDirection(newDirection);
    }

    private void AdaptVisionDistance(int nbBoidsInFieldOfView) {
        if (nbBoidsInFieldOfView > boidsManager.idealNbNeighbors 
            && visionDistance > 1){
            visionDistance--;
        } else if (nbBoidsInFieldOfView < boidsManager.idealNbNeighbors 
            && visionDistance < boidsManager.maxVisionDistance) {
            visionDistance++;
        }
    }

    private bool IsAVisibleBoid(Collider collider) {
        return !IsMyCollider(collider) && IsBoidCollider(collider) && IsInMyFieldOfView(collider);
    }

    private bool IsMyCollider(Collider collider) {
        return collider == this.GetComponent<Collider>();
    }

    private bool IsBoidCollider(Collider collider) {
        return collider.gameObject.layer == gameObject.layer;
    }

    private bool IsInMyFieldOfView(Collider collider) {
        float angle = Vector3.Angle(
            (collider.transform.position - transform.position).normalized,
            Direction
        );

        return angle <= boidsManager.visionAngle;
    }

    private Vector3 GetRandomDirection() {
        return new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }

    private void SetDirection(Vector3 newDirection) {
        Direction = newDirection;
        transform.rotation = Quaternion.LookRotation(Direction);
    }

    private void Move() {
        transform.position = transform.position + boidsManager.velocity * Direction * Time.deltaTime;
    }

    private void TeleportIfOutOfBorders() {
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
