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
    private float randomBonusVelocityFactor = 1;
    private int sinceLastBonusChange = 0;
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

    private Dictionary<long, (Vector3, Quaternion)> frameToTransform =
        new Dictionary<long, (Vector3, Quaternion)>();
    private Transform[] bones;
    private float scale;

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
        scale = transform.localScale.x;

        SetDirection(GetRandomDirection(), initialization: true);

        if (parameters.hasRig)
            bones = GetComponentInChildren<SkinnedMeshRenderer>().bones;
    }

    protected abstract void InitParams();

    protected abstract Vector3 ComputeNewDirection();

    private void FixedUpdate()
    {
        if (entitiesManager.clock % entitiesManager.calculationInterval ==
            id % entitiesManager.calculationInterval)
        {
            Vector3 optimalDirection = ComputeNewDirection();
            Vector3 adjustedDirection = IterateOnDirectionToAvoidObstacles(
                optimalDirection);
            SetDirection(adjustedDirection);
            UpdateVelocityBonusFactor();
        }

        UpdateRotation();
        AdaptVelocity();

        Move();
        AdjustPositionOfBones();
    }

    private void AdjustPositionOfBones()
    {
        if (!parameters.hasRig)
            return;

        frameToTransform[entitiesManager.clock] = (
            transform.position,
            transform.rotation
        );

        frameToTransform.Remove(entitiesManager.clock - parameters.nbPositionsToStore - 1);

        if (!frameToTransform.ContainsKey(entitiesManager.clock - parameters.nbPositionsToStore))
            return;

        for (int i = 1; i < bones.Length; i++)
        {
            float nbFrameDelay = parameters.boneDistanceToHead[i] * scale /
                (velocity * Time.fixedDeltaTime);
            (Vector3 boneNewPosition, Quaternion boneNewRotation) = FrameToTransform(
                entitiesManager.clock - nbFrameDelay);

            bones[i].rotation = boneNewRotation * parameters.boneBaseRotation[i];
            bones[i].position = boneNewPosition;
        }
    }

    private (Vector3, Quaternion) FrameToTransform(float frame)
    {
        (Vector3 firstPosition, Quaternion firstRotation) = frameToTransform[(long)Mathf.Floor(frame)];
        (Vector3 secondPosition, Quaternion secondRotation) = frameToTransform[(long)Mathf.Ceil(frame)];

        return (
            Vector3.Lerp(firstPosition, secondPosition, frame % 1f),
            Quaternion.Slerp(firstRotation, secondRotation, frame % 1f)
        );
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

    private void UpdateVelocityBonusFactor()
    {
        if (sinceLastBonusChange == parameters.nbCalculationsBetweenVelocityBonusFactorChange)
        {
            randomBonusVelocityFactor = Random.Range(
                parameters.minVelocityBonusFactor,
                parameters.maxVelocityBonusFactor
            );
            sinceLastBonusChange = 0;
        }
        else
            sinceLastBonusChange++;
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
        return toMin + (clampedValue - fromMin) * (toMax - toMin) / (fromMax - fromMin);
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
        Vector3 leftVector = Vector3.Cross(Vector3.up, Direction);
        float newTurnValue = Mathf.Clamp(Vector3.Dot(leftVector, newDirection), -1, 1);

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
        Behavior behavior, Vector3 relevantSum, float totalWeight
    )
    {
        if (totalWeight == 0f)
            return Vector3.zero;

        if (behavior == Behavior.ALIGNMENT)
        {
            Vector3 averageDirection = relevantSum.normalized;
            return averageDirection;
        }

        Vector3 averagePosition = relevantSum / totalWeight;
        Vector3 directionToAveragePosition = GetDirectionToPosition(
            averagePosition);

        if (behavior == Behavior.SEPARATION)
            return -directionToAveragePosition;

        return directionToAveragePosition;
    }

    protected float GetReelWeight(float nbInvolvedEntities, float baseWeight)
    {
        return Mathf.Min(nbInvolvedEntities, 1) * baseWeight;
    }

    protected bool Bernoulli(float probaSuccess)
    {
        float randomValue = Random.Range(0f, 1f);
        return randomValue < probaSuccess;
    }

    protected float InverseLerpOpti(float start, float end, float rangeSizeInverse, float value)
    {
        bool normalOrder = rangeSizeInverse > 0f;

        if (value <= start)
            return (normalOrder) ? 0f : 1f;

        if (value >= end)
            return (normalOrder) ? 1f : 0f;

        return (value - start) * rangeSizeInverse;
    }

    protected float GetEntityWeightAccordingToVisionDistance(float squaredDistance)
    {
        return InverseLerpOpti(
            MathHelpers.Square(visionDistance),
            MathHelpers.Square(visionDistance - 1),
            entitiesManager.visionDistanceSmoothRangeSizeInverses[visionDistance],
            squaredDistance
        );
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

        transform.rotation = Quaternion.Slerp(
            lastRotation, targetRotation, rotationProgress);

        sinceLastCalculation++;
    }

    private void Move()
    {
        transform.position = transform.position +
            velocity * Direction * Time.deltaTime;
    }

    private void AdaptVelocity()
    {
        float velocityGoal = parameters.velocities[state] * randomBonusVelocityFactor;
        float acceleration = (isItEmergencyState[state]) ?
            parameters.emergencyAcceleration : parameters.acceleration;
        float velocityStep = acceleration * Time.fixedDeltaTime;

        if (velocity > velocityGoal)
            velocity = Mathf.Max(velocity - velocityStep, velocityGoal);
        else if (velocity < velocityGoal)
            velocity = Mathf.Min(velocity + velocityStep, velocityGoal);
    }
}
