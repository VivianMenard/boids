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
    private Quaternion lastRotation;
    private Quaternion targetRotation;
    private int sinceLastCalculation;

    private enum Rule {
        SEPARATION,
        ALIGNMENT,
        COHESION
    }

    void Start() {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();
        boidsManager = GameObject.FindGameObjectWithTag("BoidsManager").
            GetComponent<BoidsManagerScript>();

        visionDistance = boidsManager.maxVisionDistance;

        SetDirection(GetRandomDirection(), initialization:true);
    }
    void Update() {}

    private void FixedUpdate() {
        if (boidsManager.clock == id % boidsManager.calculationInterval)
            ComputeNewDirection();

        UpdateRotation();

        Move();
        TeleportIfOutOfBorders();
    }

    private void ComputeNewDirection() {
        Collider[] collidersNearby = Physics.OverlapSphere(
            transform.position, 
            visionDistance
        );

        Vector3 separationPositionSum = Vector3.zero,
            alignmentDirectionSum = Vector3.zero,
            cohesionPositionSum = Vector3.zero;

        int nbBoidsSeparation = 0,
            nbBoidsAlignment = 0,
            nbBoidsCohesion = 0;

        foreach (Collider collider in collidersNearby) {
            if (!IsAVisibleBoid(collider))
                continue;

            float squaredDistance = (
                collider.transform.position - transform.position
            ).sqrMagnitude;

            if (squaredDistance > boidsManager.squaredCohesionRadius) {
                nbBoidsCohesion += 1;
                cohesionPositionSum += collider.transform.position;
            } 
            else if (squaredDistance < boidsManager.squaredSeparationRadius) {
                nbBoidsSeparation += 1;
                separationPositionSum += collider.transform.position;
            } 
            else {
                nbBoidsAlignment += 1;
                BoidScript boidScript = collider.GetComponent<BoidScript>();
                alignmentDirectionSum += boidScript.Direction;
            }
        }

        AdaptVisionDistance(nbBoidsSeparation + nbBoidsAlignment + nbBoidsCohesion);

        Vector3 separationDirection = ComputeDirectionForRule(
                Rule.SEPARATION, separationPositionSum, nbBoidsSeparation),
            alignmentDirection = ComputeDirectionForRule(
                Rule.ALIGNMENT, alignmentDirectionSum, nbBoidsAlignment),
            cohesionDirection = ComputeDirectionForRule(
                Rule.COHESION, cohesionPositionSum, nbBoidsCohesion);

        float separationCoeff = ComputeRuleCoeff(
                nbBoidsSeparation, boidsManager.separationStrengh),
            alignmentCoeff = ComputeRuleCoeff(
                nbBoidsAlignment, boidsManager.alignmentStrengh),
            cohesionCoeff = ComputeRuleCoeff(
                nbBoidsCohesion, boidsManager.cohesionStrengh);

        Vector3 newDirection = (
            (
                boidsManager.momentumStrengh * Direction + 
                separationCoeff * separationDirection +
                alignmentCoeff * alignmentDirection + 
                cohesionCoeff * cohesionDirection
            ) / (
                boidsManager.momentumStrengh + 
                separationCoeff +
                alignmentCoeff + 
                cohesionCoeff
            )
        ).normalized;

        SetDirection(newDirection);
    }

    private Vector3 ComputeDirectionForRule(Rule rule, Vector3 relevantSum, int nbInvolvedBoids) {
        if (nbInvolvedBoids == 0)
            return Vector3.zero;
        
        if (rule == Rule.ALIGNMENT) {
            Vector3 averageDirection = relevantSum.normalized;
            return averageDirection;
        }

        Vector3 averagePosition = relevantSum / (float)nbInvolvedBoids;
        Vector3 directionToAveragePosition = (averagePosition - transform.position).normalized;

        if (rule == Rule.SEPARATION)
            return -directionToAveragePosition;
        
        return directionToAveragePosition;
    }

    private float ComputeRuleCoeff(int nbInvolvedBoids, float ruleStrengh) {
        return (nbInvolvedBoids == 0) ? 0 : ruleStrengh;
    }

    private void AdaptVisionDistance(int nbBoidsInFOV) {
        if (nbBoidsInFOV > boidsManager.idealNbNeighbors 
            && visionDistance > 1){
            visionDistance--;
        } else if (nbBoidsInFOV < boidsManager.idealNbNeighbors 
            && visionDistance < boidsManager.maxVisionDistance) {
            visionDistance++;
        }
    }

    private bool IsAVisibleBoid(Collider collider) {
        return !IsMyCollider(collider) && IsBoidCollider(collider) && IsInMyFOV(collider);
    }

    private bool IsMyCollider(Collider collider) {
        return collider == this.GetComponent<Collider>();
    }

    private bool IsBoidCollider(Collider collider) {
        return collider.gameObject.layer == gameObject.layer;
    }

    private bool IsInMyFOV(Collider collider) {
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

    private void SetDirection(Vector3 newDirection, bool initialization = false) {
        Direction = newDirection;

        Quaternion newRotation = Quaternion.LookRotation(Direction);
        
        lastRotation = (initialization) ? newRotation: targetRotation;
        targetRotation = newRotation;

        sinceLastCalculation = 0;
    }

    private void UpdateRotation() {
        float rotationProgress = (float)sinceLastCalculation / (float)boidsManager.calculationInterval;
        transform.rotation = Quaternion.Lerp(lastRotation, targetRotation, rotationProgress);

        sinceLastCalculation++;
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
