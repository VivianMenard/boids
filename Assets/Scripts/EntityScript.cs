using UnityEngine;

public abstract class EntityScript : MonoBehaviour
{
    private static int nextId=0;


    public Vector3 Direction;

    protected EntitiesManagerScript entitiesManager;
    protected EntityParameters parameters;
    protected int visionDistance;
    protected bool velocityBonusActivated=false;
    protected enum Behavior {
        SEPARATION,
        ALIGNMENT,
        COHESION
    }
    
    private int id;
    private AreaScript area;
    private float velocityBonusFactor=1;
    private Quaternion lastRotation;
    private Quaternion targetRotation;
    private int sinceLastCalculation;

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

    protected abstract Vector3 ComputeNewDirection();

    private void FixedUpdate() {
        if (entitiesManager.clock == id % entitiesManager.calculationInterval) {
            Vector3 optimalDirection = ComputeNewDirection();
            Vector3 adjustedDirection = IterateOnDirectionToAvoidObstacles(optimalDirection);
            SetDirection(adjustedDirection);
        }

        UpdateRotation();
        AdaptVelocity();

        Move();
        TeleportIfOutOfBorders();
    }

    private Vector3 IterateOnDirectionToAvoidObstacles(Vector3 direction) {
        if (!entitiesManager.ObstaclesAvoidance) 
            return direction;

        RaycastHit hitInfo;
        if (PerformRaycastOnObstacles(direction, out hitInfo)) {
            (Vector3 axis1, Vector3 axis2) = CreateCoordSystemAroundVector(direction);
            float maxHitDistanceFound = 0;
            Vector3 bestDirectionFound = direction;

            int[] coords = {-1, 0, 1};
            foreach (int x in coords) foreach (int y in coords) {
                if (x == 0 && y == 0)
                    continue;
                
                Vector3 avoidanceDirection = (x * axis1 + y * axis2).normalized;
                Vector3 directionToTest = BlendAvoidanceDirectionWithDirection(
                    avoidanceDirection, direction, hitInfo.distance);

                RaycastHit testHitData;
                if (PerformRaycastOnObstacles(directionToTest, out testHitData)) {
                    if (testHitData.distance > maxHitDistanceFound) {
                        maxHitDistanceFound = testHitData.distance;
                        bestDirectionFound = directionToTest; 
                    }
                } else
                    return directionToTest;
            }
                
            return bestDirectionFound;
        }

        return direction;
    }

    private (Vector3, Vector3) CreateCoordSystemAroundVector(Vector3 axis3) {
        // axis3 needs to be normalized
        Vector3 nonColinearToAxis3 = (axis3 != Vector3.up) ? 
            Vector3.up: Vector3.right;

        Vector3 axis1 = Vector3.Cross(axis3, nonColinearToAxis3).normalized;
        Vector3 axis2 = Vector3.Cross(axis3, axis1).normalized;

        return (axis1, axis2);
    } 

    private Vector3 BlendAvoidanceDirectionWithDirection(Vector3 avoidanceDirection, Vector3 direction, float hitDistance) {
        return ((
            direction * hitDistance + 
            avoidanceDirection * (entitiesManager.raycastDistance - hitDistance)
        )/ entitiesManager.raycastDistance).normalized;
    }

    private bool PerformRaycastOnObstacles(Vector3 direction, out RaycastHit hitInfo) {
        Ray ray = new Ray(transform.position, direction);
        return Physics.Raycast(
            ray, out hitInfo, 
            entitiesManager.raycastDistance,
            entitiesManager.obstacleLayerMask
        );
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
        float cosAngle = Vector3.Dot(
            (collider.transform.position - transform.position).normalized,
            Direction
        );

        return cosAngle >= parameters.cosVisionSemiAngle;
    }

    protected void SetDirection(Vector3 newDirection, bool initialization = false) {
        Direction = newDirection;

        Quaternion newRotation = Quaternion.LookRotation(Direction);
        
        lastRotation = (initialization) ? newRotation: targetRotation;
        targetRotation = newRotation;

        sinceLastCalculation = 0;
    }

    protected Vector3 GetDirectionToPosition(Vector3 position) {
        return (position - transform.position).normalized;
    }

    protected Collider[] GetNearbyEntityColliders() {
        return Physics.OverlapSphere(
            transform.position, 
            visionDistance,
            entitiesManager.entitiesLayerMask
        );
    }

    protected Vector3 GetIdealDirectionForBehavior(Behavior behavior, Vector3 relevantSum, int nbInvolvedBoids) {
        if (nbInvolvedBoids == 0)
            return Vector3.zero;
        
        if (behavior == Behavior.ALIGNMENT) {
            Vector3 averageDirection = relevantSum.normalized;
            return averageDirection;
        }

        Vector3 averagePosition = relevantSum / (float)nbInvolvedBoids;
        Vector3 directionToAveragePosition = GetDirectionToPosition(averagePosition);

        if (behavior == Behavior.SEPARATION)
            return -directionToAveragePosition;
        
        return directionToAveragePosition;
    }

    protected float GetBehaviorWeight(int nbInvolvedEntities, float baseWeight) {
        return (nbInvolvedEntities == 0) ? 0 : baseWeight;
    }

    private Vector3 GetRandomDirection() {
        return new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }

    private void UpdateRotation() {
        float rotationProgress = (float)sinceLastCalculation / (float)entitiesManager.calculationInterval;
        transform.rotation = Quaternion.Lerp(lastRotation, targetRotation, rotationProgress);

        sinceLastCalculation++;
    }

    private void Move() {
        transform.position = transform.position + parameters.velocity * velocityBonusFactor * Direction * Time.deltaTime;
    }

    private float ComputePositionAfterTP1D(float position, float min, float max) {
        if (position < min)
            return max;
        if (position > max) 
            return min;

        return position;
    }

    private void TeleportIfOutOfBorders() {
        transform.position = new Vector3(
            ComputePositionAfterTP1D(transform.position.x, area.minPt.x, area.maxPt.x),
            ComputePositionAfterTP1D(transform.position.y, area.minPt.y, area.maxPt.y),
            ComputePositionAfterTP1D(transform.position.z, area.minPt.z, area.maxPt.z)
        );
    }

    private void AdaptVelocity() {
        if (velocityBonusActivated)
            velocityBonusFactor = Mathf.Min(
                1 + parameters.maxBonusVelocity, 
                velocityBonusFactor + parameters.velocityIncrement
            );
        else
            velocityBonusFactor = Mathf.Max(
                1, velocityBonusFactor - parameters.velocityDecrement);
    }
}
