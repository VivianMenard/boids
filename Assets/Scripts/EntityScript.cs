using System.Collections.Generic;
using UnityEngine;

/// <summary>Class whose every instance manage the behavior of an entity.</summary>
public abstract class EntityScript : MonoBehaviour
{
    private static int nextId = 0;


    protected EntitiesManagerScript entitiesManager;
    protected EntityParameters parameters;
    protected int visionDistance;

    protected State state;

    private int id;

    protected Vector3 direction;
    protected float velocity;
    private float randomBonusVelocityFactor = 1;
    private int sinceLastBonusChange = 0;

    private Quaternion lastRotation, targetRotation;
    private int sinceLastCalculation;

    // Random walk parameters
    private Vector3 rwLastDirection, rwTargetDirection;
    private int rwStateTimeRemaiming;
    protected RwState rwState = RwState.NOT_IN_RW;

    /// <summary>
    /// Dictionay to store trajectory information for the animation of the entity. Data format:
    /// (frame number) : (position at this frame, rotation at this frame, traveled distance since last frame)
    /// </summary>
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

        entitiesManager = Accessors.EntitiesManager;

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
        if (myCollider == null)
            throw new MissingComponentException(
                "No Collider component on Entity."
            );

        if (parameters.hasRig)
        {
            bones = GetComponentInChildren<SkinnedMeshRenderer>().bones;
            bonesPositionsAndRotations = new (Vector3, Quaternion)[bones.Length];

            CreateFakeTransformHistory();
        }
    }

    /// <summary>Initializes entity parameters.</summary>
    protected abstract void InitParams();

    /// <summary>
    /// Computes the new optimal direction for the entity based on its state and the other entities that surround it.
    /// </summary>
    protected abstract Vector3 ComputeNewDirection();

    /// <summary>Computes the initial direction of the entity based on its spawn rotation.</summary>
    private Vector3 GetInitialDirection()
    {
        return MathHelpers.RotationToDirection(myRotation);
    }

    /// <summary>
    /// Used just after the spawn of the entity to create a fake trajectory history for the entity, to be able to begin animate it.
    /// </summary>
    private void CreateFakeTransformHistory()
    {
        for (int fakeFrame = 0; fakeFrame < parameters.nbTransformsToStore; fakeFrame++)
        {
            frameToTransformInfo[entitiesManager.Clock - fakeFrame] = (
                myPosition - velocity * fakeFrame * Time.fixedDeltaTime * direction,
                myRotation,
                velocity * Time.fixedDeltaTime
            );
        }
    }

    private void FixedUpdate()
    {
        if (!entitiesManager.EntitiesMovement)
            return;

        if (entitiesManager.Clock % entitiesManager.CalculationInterval ==
            id % entitiesManager.CalculationInterval)
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
        ManageAnimation();
    }

    /// <summary>
    /// Handles all the stuff related to entity animation. Stores the current position/rotation in the trajectory history, and 
    /// use this history to animate the bones.
    /// </summary>
    private void ManageAnimation()
    {
        if (!parameters.hasRig)
            return;

        StoreTransformInfo();

        if (!frameToTransformInfo.ContainsKey(entitiesManager.Clock - parameters.nbTransformsToStore))
            return;

        ComputeBonesPositionsAndRotations();
        ApplyBonesPositionsAndRotations();
    }

    /// <summary>
    /// Stores the current relevant pieces of information in the trajectory history. Removes history entries that are now too
    /// old to be relevant.
    /// </summary>
    private void StoreTransformInfo()
    {
        frameToTransformInfo[entitiesManager.Clock] = (
            myPosition,
            myRotation,
            velocity * Time.fixedDeltaTime
        );

        frameToTransformInfo.Remove(entitiesManager.Clock - parameters.nbTransformsToStore - 1);
    }

    /// <summary>
    /// Applies position and rotation stored in <c>bonesPositionsAndRotations</c> to the bones of the entity.
    /// </summary>
    private void ApplyBonesPositionsAndRotations()
    {
        for (int boneIndex = parameters.animationFirstBone; boneIndex < bones.Length; boneIndex++)
        {
            (Vector3 boneNewPosition, Quaternion boneNewRotation) = bonesPositionsAndRotations[boneIndex];

            bones[boneIndex].rotation = boneNewRotation * parameters.boneBaseRotation[boneIndex];
            bones[boneIndex].position = boneNewPosition;
        }
    }

    /// <summary>
    /// Adapts obstacles avoidance parameters to the current velocity of the entity 
    /// (the higher the entity velocity is, the higher the raycast distance need to be to detect obstacles on time).
    /// </summary>
    private void AdaptObstacleAvoidanceParams()
    {
        float velocityFactor = velocity / parameters.velocities[parameters.defaultState];

        obstacleMargin = velocityFactor * parameters.obstacleBaseMargin;
        raycastDistance = velocityFactor * parameters.raycastBaseDistance;
    }

    /// <summary>
    /// Computes the ideal position of each bone according the entity trajectory history.
    /// </summary>
    protected virtual void ComputeBonesPositionsAndRotations()
    {
        float traveledDistance = 0f;
        int currentBone = parameters.animationFirstBone;
        int currentFrameOffset = 0;

        while (currentBone < bones.Length)
        {
            (Vector3 pastPosition, Quaternion pastRotation, float frameTraveledDistance) =
                frameToTransformInfo[entitiesManager.Clock - currentFrameOffset];

            if (traveledDistance + frameTraveledDistance >= BoneDistanceToHead(currentBone))
            {
                (Vector3 pastPastPosition, Quaternion pastPastRotation, float _) =
                    frameToTransformInfo[entitiesManager.Clock - currentFrameOffset - 1];
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

    /// <summary>Compute the distance between a specific bone and the head of the entity.</summary>
    /// <param name="boneIndex">The index of the bone.</param>
    /// <returns>The distance between the bone ans the head.</returns>
    protected float BoneDistanceToHead(int boneIndex)
    {
        return myScale * parameters.boneBaseDistanceToHead[boneIndex];
    }

    /// <summary>
    /// Computes a new direction for the entity to make it adopt a random walk behavior.
    /// The resulting behavior will be globally random but locally coherent. The entity will
    /// chain straight lines and curves.
    /// </summary>
    /// <returns>The direction for a random walk behavior</returns> 
    protected Vector3 RandomWalk() // is abreviated by 'rw' in the rest of the code
    {
        if (rwState == RwState.NOT_IN_RW || rwStateTimeRemaiming == 0)
        {
            rwStateTimeRemaiming = parameters.rwStatePeriod;

            if (MathHelpers.Bernoulli(parameters.rwProbaStraightLine))
                rwState = RwState.STRAIGHT_LINE;
            else
            {
                rwState = RwState.DIRECTION_CHANGE;

                rwLastDirection = direction;
                rwTargetDirection = GetNewTargetDirectionForRw();
            }
        }

        Vector3 newDirection = direction;

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

    /// <summary>
    /// Updates the multiplicative bonus factor of the entity if the bonus factor cycle is ended.
    /// </summary>
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


    /// /// <summary>
    /// Finds a new target direction for random walk behavior. Tries to find one that doesn't lead to an obstacle.
    /// </summary>
    /// <returns>A new target direction for random walk behavior.</returns> 
    private Vector3 GetNewTargetDirectionForRw()
    {
        Vector3 TryDirectionForRw()
        {
            return (MathHelpers.GetRandomDirection(parameters.rwVerticalRestriction) +
                direction * parameters.rwMomentumWeight).normalized;
        }

        Vector3 directionForRw = TryDirectionForRw();
        int nbAttempts = 0;
        RaycastHit hitInfo;


        // Even if this part may seem redondant with the one that avoid obstacles the goal is different.
        // If the entity is close to the wall and the random walk function leads it trough the wall the 
        // IterateOnDirectionToAvoidObstacles function will avoid the collision by making the entity go 
        // along the wall. So without this code an entity close to the wall has 1/2 chance to just 
        // follow it, which is visually boring.
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

    /// <summary>Corrects a potential entity direction to take obstacles into account.</summary>
    /// <param name="initialDirection">The initial direction.</param>
    /// <returns>The new direction that avoid obstacles if needed.</returns>
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

    /// <summary>
    /// Computes 8 potential avoidance directions around the initial one.
    /// The result isn't returned but stored in <c>avoidanceDirections</c> attribute to avoid constantly reallocating memory.
    /// </summary>
    /// <param name="initialDirection">The initial direction.</param>
    private void ComputeAvoidanceDirections(Vector3 initialDirection)
    {
        (Vector3 axis1, Vector3 axis2) = MathHelpers.GenerateOrthogonalAxesAroundVector(
            initialDirection, GetObstacleAvoidanceReference());

        avoidanceDirections[0] = axis1;
        avoidanceDirections[1] = -axis1;
        avoidanceDirections[2] = MathHelpers.OneOverSquareRootOfTwo * (axis1 + axis2);
        avoidanceDirections[3] = MathHelpers.OneOverSquareRootOfTwo * (axis1 - axis2);
        avoidanceDirections[4] = MathHelpers.OneOverSquareRootOfTwo * (-axis1 + axis2);
        avoidanceDirections[5] = MathHelpers.OneOverSquareRootOfTwo * (-axis1 - axis2);
        avoidanceDirections[6] = axis2;
        avoidanceDirections[7] = -axis2;
    }

    protected abstract Vector3 GetObstacleAvoidanceReference();

    private Vector3 BlendAvoidanceDirectionWithDirection(
        Vector3 avoidanceDirection, Vector3 initialDirection, float hitDistance
    )
    {
        float perceivedDistance = MathHelpers.Remap(
            hitDistance, obstacleMargin, raycastDistance, 0, raycastDistance);

        return (
            initialDirection * perceivedDistance +
            avoidanceDirection * (raycastDistance - perceivedDistance)
        ).normalized;
    }

    private bool PerformRaycastOnObstacles(Vector3 raycastDirection, out RaycastHit hitInfo)
    {
        Ray ray = new Ray(myPosition, raycastDirection);

        return Physics.Raycast(
            ray, out hitInfo,
            raycastDistance,
            parameters.obstaclesLayerMask
        );
    }

    protected bool IsInMyFOV(Vector3 entityPosition)
    {
        float cosAngle = Vector3.Dot(
            (entityPosition - myPosition).normalized,
            direction
        );

        return cosAngle >= parameters.cosVisionSemiAngle;
    }

    private void SetNewDirectionTarget(Vector3 newDirection, bool initialization = false)
    {
        if (initialization)
            direction = newDirection;

        Quaternion newRotation = Quaternion.LookRotation(newDirection);

        lastRotation = (initialization) ? newRotation : targetRotation;
        targetRotation = newRotation;

        sinceLastCalculation = 0;
    }

    protected Collider[] GetNearbyEntityColliders()
    {
        return Physics.OverlapSphere(
            myPosition,
            visionDistance,
            entitiesManager.EntitiesLayerMask
        );
    }

    protected Vector3 GetIdealDirectionForBehavior(
        Behavior behavior, Vector3 relevantSum, float totalWeight
    )
    {
        if (totalWeight < Mathf.Epsilon)
            return Vector3.zero;

        if (behavior == Behavior.ALIGNMENT)
        {
            Vector3 averageDirection = relevantSum.normalized;
            return averageDirection;
        }

        Vector3 averagePosition = relevantSum / totalWeight;
        Vector3 directionToAveragePosition =
            (averagePosition - myPosition).normalized;

        if (behavior == Behavior.SEPARATION)
            return -directionToAveragePosition;

        return directionToAveragePosition;
    }

    protected float GetReelWeight(float nbInvolvedEntities, float baseWeight)
    {
        return Mathf.Min(nbInvolvedEntities, 1) * baseWeight;
    }

    protected float GetEntityWeightAccordingToVisionDistance(float squaredDistance)
    {
        return Mathf.InverseLerp(
            MathHelpers.Square(visionDistance),
            MathHelpers.Square(visionDistance - 1),
            squaredDistance
        );
    }

    private void UpdateDirectionAndRotation()
    {
        float rotationProgress = (float)sinceLastCalculation /
            (float)entitiesManager.CalculationInterval;

        Quaternion newRotation = Quaternion.Slerp(
            lastRotation, targetRotation, rotationProgress);

        transform.rotation = newRotation;
        myRotation = newRotation;

        direction = MathHelpers.RotationToDirection(newRotation);
        // the direction is based on the rotation and not the contrary, it's intentionnal.
        // It allows the rotation twists to be smooth, what wouldn't be the case if the lerp
        // was on the direction and the rotation was based on the result

        entitiesManager.UpdateEntityDirection(myCollider, direction);

        sinceLastCalculation++;
    }

    private void Move()
    {
        Vector3 newPosition = myPosition + velocity * Time.fixedDeltaTime * direction;

        transform.position = newPosition;
        myPosition = newPosition;
        entitiesManager.UpdateEntityPosition(myCollider, newPosition);
    }

    private void AdaptVelocity()
    {
        float velocityGoal = parameters.velocities[state] * randomBonusVelocityFactor;
        float acceleration = (entitiesManager.IsItEmergencyState[state]) ?
            parameters.emergencyAcceleration : parameters.acceleration;
        float velocityStep = acceleration * Time.fixedDeltaTime;

        if (velocity > velocityGoal)
            velocity = Mathf.Max(velocity - velocityStep, velocityGoal);
        else if (velocity < velocityGoal)
            velocity = Mathf.Min(velocity + velocityStep, velocityGoal);

        if (parameters.applyVelocityFactor)
            AdaptObstacleAvoidanceParams();
    }

    private void OnDestroy()
    {
        entitiesManager.RemoveAssociatedEntries(myCollider);
    }
}
