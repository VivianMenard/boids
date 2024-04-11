using System.Collections.Generic;
using UnityEngine;


public abstract class EntityScript : MonoBehaviour
{
    private static int nextId = 0;


    public Vector3 Direction;

    protected EntitiesManagerScript entitiesManager;
    protected EntityParameters parameters;
    protected int visionDistance;

    protected State state;

    private int id;

    protected float velocity;
    private float randomBonusVelocityFactor = 1;
    private int sinceLastBonusChange = 0;

    private Quaternion lastRotation, targetRotation;
    private int sinceLastCalculation;

    // Random walk parameters
    private Vector3 rwLastDirection, rwTargetDirection;
    private int rwStateTimeRemaiming;
    protected RwState rwState = RwState.NOT_IN_RW;

    // data format : (frame number) : (position at this frame, rotation at this frame, traveled distance since last frame)
    private Dictionary<long, (Vector3, Quaternion, float)> frameToTransformInfo =
        new Dictionary<long, (Vector3, Quaternion, float)>();
    protected (Vector3, Quaternion)[] bonesPositionsAndRotations;
    protected Transform[] bones;

    private float myScale;
    protected Vector3 myPosition;
    private Quaternion myRotation;

    protected Collider myCollider;

    private float raycastDistance, obstacleMargin;
    private Vector3[] avoidanceDirections = new Vector3[8];

    void Start()
    {
        id = nextId++;

        entitiesManager = GameObject.FindGameObjectWithTag(Constants.entitiesManagerTag).
            GetComponent<EntitiesManagerScript>();

        InitParams();

        visionDistance = parameters.visionDistance;
        state = parameters.defaultState;
        velocity = parameters.velocities[state];
        raycastDistance = parameters.raycastBaseDistance;
        obstacleMargin = parameters.obstacleBaseMargin;

        myScale = transform.localScale.x;
        myPosition = transform.position;
        myRotation = transform.rotation;

        SetNewDirectionTarget(GetInitialDirection(), initialization: true);

        myCollider = GetComponent<Collider>();

        if (parameters.hasRig)
        {
            bones = GetComponentInChildren<SkinnedMeshRenderer>().bones;
            bonesPositionsAndRotations = new (Vector3, Quaternion)[bones.Length];

            CreateFakeTransformHistory();
        }
    }

    protected abstract void InitParams();

    protected abstract Vector3 ComputeNewDirection();

    private Vector3 GetInitialDirection()
    {
        return MathHelpers.RotationToDirection(myRotation);
    }

    private void CreateFakeTransformHistory()
    {
        for (int fakeFrame = 0; fakeFrame < parameters.nbTransformsToStore; fakeFrame++)
        {
            frameToTransformInfo[entitiesManager.clock - fakeFrame] = (
                myPosition - velocity * fakeFrame * Time.fixedDeltaTime * Direction,
                myRotation,
                velocity * Time.fixedDeltaTime
            );
        }
    }

    private void FixedUpdate()
    {
        if (!entitiesManager.entitiesMovement)
            return;

        if (entitiesManager.clock % entitiesManager.calculationInterval ==
            id % entitiesManager.calculationInterval)
        {
            Vector3 optimalDirection = ComputeNewDirection();
            Vector3 adjustedDirection = IterateOnDirectionToAvoidObstacles(
                optimalDirection);
            SetNewDirectionTarget(adjustedDirection);
            UpdateVelocityBonusFactor();
        }

        UpdateDirectionAndRotation();
        AdaptVelocity();

        Move();
        ManageBones();
    }

    private void ManageBones()
    {
        if (!parameters.hasRig)
            return;

        StoreTransformInfo();

        if (!frameToTransformInfo.ContainsKey(entitiesManager.clock - parameters.nbTransformsToStore))
            return;

        ComputeBonesPositionsAndRotations();
        ApplyBonesPositionsAndRotations();
    }

    private void StoreTransformInfo()
    {
        frameToTransformInfo[entitiesManager.clock] = (
            myPosition,
            myRotation,
            velocity * Time.fixedDeltaTime
        );

        frameToTransformInfo.Remove(entitiesManager.clock - parameters.nbTransformsToStore - 1);
    }

    private void ApplyBonesPositionsAndRotations()
    {
        for (int boneIndex = parameters.animationFirstBone; boneIndex < bones.Length; boneIndex++)
        {
            (Vector3 boneNewPosition, Quaternion boneNewRotation) = bonesPositionsAndRotations[boneIndex];

            bones[boneIndex].rotation = boneNewRotation * parameters.boneBaseRotation[boneIndex];
            bones[boneIndex].position = boneNewPosition;
        }
    }

    private void AdaptObstacleAvoidanceParams()
    {
        float velocityFactor = velocity / parameters.velocities[parameters.defaultState];

        obstacleMargin = velocityFactor * parameters.obstacleBaseMargin;
        raycastDistance = velocityFactor * parameters.raycastBaseDistance;
    }

    protected virtual void ComputeBonesPositionsAndRotations()
    {
        float traveledDistance = 0f;
        int currentBone = parameters.animationFirstBone;
        int currentFrameOffset = 0;

        while (currentBone < bones.Length)
        {
            (Vector3 pastPosition, Quaternion pastRotation, float frameTraveledDistance) =
                frameToTransformInfo[entitiesManager.clock - currentFrameOffset];

            if (traveledDistance + frameTraveledDistance >= BoneDistanceToHead(currentBone))
            {
                (Vector3 pastPastPosition, Quaternion pastPastRotation, float _) =
                    frameToTransformInfo[entitiesManager.clock - currentFrameOffset - 1];
                float frameFraction = (BoneDistanceToHead(currentBone) - traveledDistance) /
                    frameTraveledDistance;
                bonesPositionsAndRotations[currentBone] = (
                    Vector3.Lerp(pastPosition, pastPastPosition, frameFraction),
                    Quaternion.Slerp(pastRotation, pastPastRotation, frameFraction)
                );
                currentBone++;
            }
            else
            {
                traveledDistance += frameTraveledDistance;
                currentFrameOffset++;
            }
        }
    }

    protected float BoneDistanceToHead(int boneIndex)
    {
        return myScale * parameters.boneBaseDistanceToHead[boneIndex];
    }

    private (Vector3, Quaternion) FrameToTransform(float frame)
    {
        (Vector3 firstPosition, Quaternion firstRotation, float _) =
            frameToTransformInfo[(long)Mathf.Floor(frame)];
        (Vector3 secondPosition, Quaternion secondRotation, float _) =
            frameToTransformInfo[(long)Mathf.Ceil(frame)];

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
            Vector3 newDirection = MathHelpers.GetRandomDirection() +
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

    private Vector3 IterateOnDirectionToAvoidObstacles(Vector3 initialDirection)
    {
        RaycastHit hitInfo;
        if (PerformRaycastOnObstacles(initialDirection, out hitInfo))
        {
            rwState = RwState.NOT_IN_RW;

            float maxHitDistanceFound = 0;
            Vector3 bestDirectionFound = initialDirection;
            ComputeAvoidanceDirections(initialDirection);

            for (int directionIndex = 0; directionIndex < avoidanceDirections.Length; directionIndex++)
            {
                Vector3 avoidanceDirection = avoidanceDirections[directionIndex];
                float preference = parameters.avoidanceDirectionPreferences[directionIndex];

                Vector3 directionToTest = BlendAvoidanceDirectionWithDirection(
                    avoidanceDirection, initialDirection, hitInfo.distance);

                RaycastHit testHitData;
                if (PerformRaycastOnObstacles(directionToTest, out testHitData))
                {
                    float weightedDistance = testHitData.distance * preference;
                    if (weightedDistance > maxHitDistanceFound)
                    {
                        maxHitDistanceFound = weightedDistance;
                        bestDirectionFound = directionToTest;
                    }
                }
                else
                    return directionToTest;
            }

            return bestDirectionFound;
        }

        return initialDirection;
    }

    private void ComputeAvoidanceDirections(Vector3 initialDirection)
    {
        (Vector3 axis1, Vector3 axis2) = CreateCoordSystemForAbstacleAvoidance(initialDirection);

        avoidanceDirections[0] = axis1;
        avoidanceDirections[1] = -axis1;
        avoidanceDirections[2] = MathHelpers.OneOverSquareRootOfTwo * (axis1 + axis2);
        avoidanceDirections[3] = MathHelpers.OneOverSquareRootOfTwo * (axis1 - axis2);
        avoidanceDirections[4] = MathHelpers.OneOverSquareRootOfTwo * (-axis1 + axis2);
        avoidanceDirections[5] = MathHelpers.OneOverSquareRootOfTwo * (-axis1 - axis2);
        avoidanceDirections[6] = axis2;
        avoidanceDirections[7] = -axis2;
    }

    private (Vector3, Vector3) CreateCoordSystemForAbstacleAvoidance(Vector3 axis3)
    {
        Vector3 axis1 = Vector3.Cross(axis3, GetObstacleAvoidanceReference()).normalized;
        Vector3 axis2 = Vector3.Cross(axis3, axis1).normalized;

        return (axis1, axis2);
    }

    protected abstract Vector3 GetObstacleAvoidanceReference();

    private Vector3 BlendAvoidanceDirectionWithDirection(
        Vector3 avoidanceDirection, Vector3 initialDirection, float hitDistance
    )
    {
        float perceivedDistance = Remap(
            hitDistance, obstacleMargin, raycastDistance, 0, raycastDistance);

        return (
            initialDirection * perceivedDistance +
            avoidanceDirection * (raycastDistance - perceivedDistance)
        ).normalized;
    }

    private float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float clampedValue = Mathf.Clamp(value, fromMin, fromMax);
        return toMin + (clampedValue - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

    private bool PerformRaycastOnObstacles(Vector3 direction, out RaycastHit hitInfo)
    {
        Ray ray = new Ray(myPosition, direction);

        return Physics.Raycast(
            ray, out hitInfo,
            raycastDistance,
            entitiesManager.obstacleLayerMask
        );
    }

    protected bool IsInMyFOV(Vector3 entityPosition)
    {
        float cosAngle = Vector3.Dot(
            (entityPosition - myPosition).normalized,
            Direction
        );

        return cosAngle >= parameters.cosVisionSemiAngle;
    }

    private void SetNewDirectionTarget(Vector3 newDirection, bool initialization = false)
    {
        if (initialization)
            Direction = newDirection;

        Quaternion newRotation = Quaternion.LookRotation(newDirection);

        lastRotation = (initialization) ? newRotation : targetRotation;
        targetRotation = newRotation;

        sinceLastCalculation = 0;
    }

    protected Vector3 GetDirectionToSpecificPosition(Vector3 specificPosition)
    {
        return (specificPosition - myPosition).normalized;
    }

    protected Collider[] GetNearbyEntityColliders()
    {
        return Physics.OverlapSphere(
            myPosition,
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
        Vector3 directionToAveragePosition = GetDirectionToSpecificPosition(
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

    private void UpdateDirectionAndRotation()
    {
        float rotationProgress = (float)sinceLastCalculation /
            (float)entitiesManager.calculationInterval;

        Quaternion newRotation = Quaternion.Slerp(
            lastRotation, targetRotation, rotationProgress);

        transform.rotation = newRotation;
        myRotation = newRotation;

        Direction = MathHelpers.RotationToDirection(newRotation);
        // the direction is based on the rotation and not the contrary, it's intentionnal.
        // It allows the rotation twists to be smooth, what wouldn't be the case if the lerp
        // was on the direction and the rotation was based on the result

        sinceLastCalculation++;
    }

    private void Move()
    {
        Vector3 newPosition = myPosition + velocity * Time.fixedDeltaTime * Direction;

        transform.position = newPosition;
        myPosition = newPosition;
    }

    private void AdaptVelocity()
    {
        float velocityGoal = parameters.velocities[state] * randomBonusVelocityFactor;
        float acceleration = (entitiesManager.isItEmergencyState[state]) ?
            parameters.emergencyAcceleration : parameters.acceleration;
        float velocityStep = acceleration * Time.fixedDeltaTime;

        if (velocity > velocityGoal)
            velocity = Mathf.Max(velocity - velocityStep, velocityGoal);
        else if (velocity < velocityGoal)
            velocity = Mathf.Min(velocity + velocityStep, velocityGoal);

        if (parameters.applyVelocityFactor)
            AdaptObstacleAvoidanceParams();
    }
}
