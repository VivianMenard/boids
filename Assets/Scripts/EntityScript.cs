using System.Collections.Generic;
using UnityEngine;

public enum State
{
    NORMAL,
    ALONE,
    AFRAID,
    CHILLING,
    HUNTING,
    ATTACKING
}

public abstract class EntityScript : MonoBehaviour
{
    private static int nextId = 0;


    public Vector3 Direction;

    protected EntitiesManagerScript entitiesManager;
    protected EntityParameters parameters;
    protected int visionDistance;
    protected enum Behavior
    {
        SEPARATION,
        ALIGNMENT,
        COHESION
    }
    protected State state;

    private int id;
    private float velocity;
    private AreaScript area;
    private Quaternion lastRotation;
    private Quaternion targetRotation;
    private int sinceLastCalculation;
    private Dictionary<State, bool> isItEmergencyState = new Dictionary<State, bool>{
        {State.NORMAL, false},
        {State.ALONE, false},
        {State.AFRAID, true},
        {State.CHILLING, false},
        {State.HUNTING, false},
        {State.ATTACKING, true}
    };

    // Random walk parameters
    private Vector3 rwLastDirection;
    private Vector3 rwTargetDirection;
    private int rwStateTimeRemaiming;
    protected enum RwState
    {
        STRAIGHT_LINE,
        DIRECTION_CHANGE,
        NOT_IN_RW
    }
    protected RwState rwState = RwState.NOT_IN_RW;

    void Start()
    {
        id = nextId++;

        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();
        entitiesManager = GameObject.FindGameObjectWithTag("BoidsManager").
            GetComponent<EntitiesManagerScript>();

        InitParams();

        visionDistance = parameters.visionDistance;
        velocity = parameters.velocities[state];

        SetDirection(GetRandomDirection(), initialization: true);
    }

    protected abstract void InitParams();

    protected abstract Vector3 ComputeNewDirection();

    private void FixedUpdate()
    {
        if (entitiesManager.clock == id % entitiesManager.calculationInterval)
        {
            Vector3 optimalDirection = ComputeNewDirection();
            Vector3 adjustedDirection = IterateOnDirectionToAvoidObstacles(
                optimalDirection);
            SetDirection(adjustedDirection);
        }

        UpdateRotation();
        AdaptVelocity();

        Move();
        TeleportIfOutOfBorders();
    }

    protected Vector3 RandomWalk() // is abreviated by 'rw' in the rest of the code
    {
        if (rwState == RwState.NOT_IN_RW || rwStateTimeRemaiming == 0)
        {
            rwStateTimeRemaiming = parameters.rwStatePeriod;

            if (Bernoulli(parameters.rwProbaStraightLine))
                rwState = RwState.STRAIGHT_LINE;
            else
            {
                rwState = RwState.DIRECTION_CHANGE;

                rwLastDirection = Direction;
                rwTargetDirection = GetDirectionForRw();
            }
        }

        Vector3 newDirection = Direction;

        if (rwState == RwState.DIRECTION_CHANGE)
        {
            float progress = (float)(parameters.rwStatePeriod - rwStateTimeRemaiming) /
                (float)parameters.rwStatePeriod;
            newDirection = Vector3.Lerp(
                rwLastDirection, rwTargetDirection, progress
            ).normalized;
        }

        rwStateTimeRemaiming--;
        return newDirection;
    }

    private Vector3 GetDirectionForRw()
    {
        Vector3 TryDirectionForRw()
        {
            Vector3 newDirection = GetRandomDirection() +
                Direction * parameters.rwMomentumWeight;
            newDirection.y = newDirection.y * parameters.rwVerticalDirFactor;
            return newDirection.normalized;
        }

        Vector3 directionForRw = TryDirectionForRw();
        int nbAttempts = 0;
        RaycastHit hitInfo;

        while (
            PerformRaycastOnObstacles(directionForRw, out hitInfo) &&
            nbAttempts < parameters.rwMaxAttempts
        )
        {
            directionForRw = TryDirectionForRw();
            nbAttempts++;
        }

        return directionForRw;
    }

    private Vector3 IterateOnDirectionToAvoidObstacles(Vector3 direction)
    {
        if (!entitiesManager.ObstaclesAvoidance)
            return direction;

        RaycastHit hitInfo;
        if (PerformRaycastOnObstacles(direction, out hitInfo))
        {
            rwState = RwState.NOT_IN_RW;

            (Vector3 axis1, Vector3 axis2) = CreateCoordSystemAroundVector(direction);
            float maxHitDistanceFound = 0;
            Vector3 bestDirectionFound = direction;

            int[] coords = { -1, 0, 1 };
            foreach (int x in coords) foreach (int y in coords)
                {
                    if (x == 0 && y == 0)
                        continue;

                    Vector3 avoidanceDirection = (x * axis1 + y * axis2).normalized;
                    Vector3 directionToTest = BlendAvoidanceDirectionWithDirection(
                        avoidanceDirection, direction, hitInfo.distance);

                    RaycastHit testHitData;
                    if (PerformRaycastOnObstacles(directionToTest, out testHitData))
                    {
                        if (testHitData.distance > maxHitDistanceFound)
                        {
                            maxHitDistanceFound = testHitData.distance;
                            bestDirectionFound = directionToTest;
                        }
                    }
                    else
                        return directionToTest;
                }

            return bestDirectionFound;
        }

        return direction;
    }

    private (Vector3, Vector3) CreateCoordSystemAroundVector(Vector3 axis3)
    {
        Vector3 axis1 = Vector3.Cross(axis3, GetRandomDirection()).normalized;
        Vector3 axis2 = Vector3.Cross(axis3, axis1).normalized;

        return (axis1, axis2);
    }

    private Vector3 BlendAvoidanceDirectionWithDirection(
        Vector3 avoidanceDirection, Vector3 direction, float hitDistance
    )
    {
        float perceivedDistance = Remap(hitDistance,
            entitiesManager.obstacleMargin, entitiesManager.raycastDistance,
            0, entitiesManager.raycastDistance
        );

        return (
            direction * perceivedDistance +
            avoidanceDirection * (entitiesManager.raycastDistance - perceivedDistance)
        ).normalized;
    }

    private float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float clampedValue = Mathf.Clamp(value, fromMin, fromMax);
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

    private bool PerformRaycastOnObstacles(Vector3 direction, out RaycastHit hitInfo)
    {
        Ray ray = new Ray(transform.position, direction);
        return Physics.Raycast(
            ray, out hitInfo,
            entitiesManager.raycastDistance,
            entitiesManager.obstacleLayerMask
        );
    }

    protected bool IsMyCollider(Collider collider)
    {
        return collider == this.GetComponent<Collider>();
    }

    protected bool IsBoidCollider(Collider collider)
    {
        return collider.gameObject.layer == LayerMask.NameToLayer("Boids");
    }

    protected bool IsPredatorCollider(Collider collider)
    {
        return collider.gameObject.layer == LayerMask.NameToLayer("Predators");
    }

    protected bool IsInMyFOV(Collider collider)
    {
        float cosAngle = Vector3.Dot(
            (collider.transform.position - transform.position).normalized,
            Direction
        );

        return cosAngle >= parameters.cosVisionSemiAngle;
    }

    protected void SetDirection(Vector3 newDirection, bool initialization = false)
    {
        Direction = newDirection;

        Quaternion newRotation = Quaternion.LookRotation(Direction);

        lastRotation = (initialization) ? newRotation : targetRotation;
        targetRotation = newRotation;

        sinceLastCalculation = 0;
    }

    protected Vector3 GetDirectionToPosition(Vector3 position)
    {
        return (position - transform.position).normalized;
    }

    protected Collider[] GetNearbyEntityColliders()
    {
        return Physics.OverlapSphere(
            transform.position,
            visionDistance,
            entitiesManager.entitiesLayerMask
        );
    }

    protected Vector3 GetIdealDirectionForBehavior(
        Behavior behavior, Vector3 relevantSum, int nbInvolvedBoids
    )
    {
        if (nbInvolvedBoids == 0)
            return Vector3.zero;

        if (behavior == Behavior.ALIGNMENT)
        {
            Vector3 averageDirection = relevantSum.normalized;
            return averageDirection;
        }

        Vector3 averagePosition = relevantSum / (float)nbInvolvedBoids;
        Vector3 directionToAveragePosition = GetDirectionToPosition(
            averagePosition);

        if (behavior == Behavior.SEPARATION)
            return -directionToAveragePosition;

        return directionToAveragePosition;
    }

    protected float GetBehaviorWeight(int nbInvolvedEntities, float baseWeight)
    {
        return (nbInvolvedEntities == 0) ? 0 : baseWeight;
    }

    protected bool Bernoulli(float probaSuccess)
    {
        float randomValue = Random.Range(0f, 1f);
        return randomValue < probaSuccess;
    }

    private Vector3 GetRandomDirection()
    {
        float theta = Random.Range(0f, 2f * Mathf.PI);
        float phi = Random.Range(0f, Mathf.PI);

        return new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Sin(phi) * Mathf.Sin(theta),
            Mathf.Cos(phi)
        );
    }

    private void UpdateRotation()
    {
        float rotationProgress = (float)sinceLastCalculation /
            (float)entitiesManager.calculationInterval;
        transform.rotation = Quaternion.Lerp(
            lastRotation, targetRotation, rotationProgress);

        sinceLastCalculation++;
    }

    private void Move()
    {
        transform.position = transform.position +
            velocity * Direction * Time.deltaTime;
    }

    private float ComputePositionAfterTP1D(float position, float min, float max)
    {
        if (position < min)
            return max;
        if (position > max)
            return min;

        return position;
    }

    private void TeleportIfOutOfBorders()
    {
        transform.position = new Vector3(
            ComputePositionAfterTP1D(transform.position.x, area.minPt.x, area.maxPt.x),
            ComputePositionAfterTP1D(transform.position.y, area.minPt.y, area.maxPt.y),
            ComputePositionAfterTP1D(transform.position.z, area.minPt.z, area.maxPt.z)
        );
    }

    private void AdaptVelocity()
    {
        float velocityGoal = parameters.velocities[state];
        float acceleration = (isItEmergencyState[state]) ?
            parameters.emergencyAcceleration : parameters.acceleration;
        float velocityStep = acceleration * Time.fixedDeltaTime;

        if (velocity > velocityGoal)
            velocity = Mathf.Max(velocity - velocityStep, velocityGoal);
        else if (velocity < velocityGoal)
            velocity = Mathf.Min(velocity + velocityStep, velocityGoal);
    }
}
