using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class EntityParameters
{
    public GameObject prefab;

    [Space, Range(0, 1)]
    public float scaleVariations;

    [Space, Range(0, 30)]
    public float raycastBaseDistance;
    [Range(0, 5), Tooltip("The distance entities will try to keep between them and the obstacle")]
    public float obstacleBaseMargin;
    [Tooltip("If yes, obstacle avoidance parameters will be proportional to entity velocity. Must not be switched during playmode")]
    public bool applyVelocityFactor;

    [Space, Range(0, 50), Tooltip("In u/s²")]
    public float acceleration;
    [Range(0, 50), Tooltip("In u/s²")]
    public float emergencyAcceleration;
    [Range(0, 10), Tooltip("In seconds")]
    public float velocityBonusFactorChangePeriod;
    [HideInInspector]
    public int nbCalculationsBetweenVelocityBonusFactorChange;
    [Range(0, 1)]
    public float minVelocityBonusFactor;
    [Range(1, 2)]
    public float maxVelocityBonusFactor;


    [Space, Range(0, 50), Tooltip("In random walk: Number of fixed updates between state changes")]
    public int rwStatePeriod;
    [Range(0, 5), Tooltip("In random walk: Tendency to choose a new direction close to the old one")]
    public float rwMomentumWeight;
    [Range(0, 1), Tooltip("In random walk: probability to go straight")]
    public float rwProbaStraightLine;
    [Range(0, 1), Tooltip("In random walk: allows to avoid vertical directions if close to 0")]
    public float rwVerticalDirFactor;
    [Range(0, 10), Tooltip("In random walk: Max number of attempts to find a new random direction without obstacles")]
    public int rwMaxAttempts;

    [Space, Range(0, 15)]
    public int visionDistance;
    [Range(0, 180)]
    public int visionSemiAngle;
    [HideInInspector]
    public float cosVisionSemiAngle;

    [Space, Range(0, 10), Tooltip("Tendency to choose a new direction close to the old one")]
    public float momentumWeight;

    [HideInInspector]
    public Dictionary<State, float> velocities;
    [HideInInspector]
    public State defaultState;

    [HideInInspector]
    public bool hasRig;
    [HideInInspector]
    public float[] boneBaseDistanceToHead;
    [HideInInspector]
    public int nbTransformsToStore;
    [HideInInspector]
    public Quaternion[] boneBaseRotation;
    [HideInInspector]
    public int animationFirstBone;

    public virtual void PreCalculateParameters(int calculationInterval, float smoothnessRadiusOffset)
    {
        cosVisionSemiAngle = Mathf.Cos(visionSemiAngle * Mathf.Deg2Rad);

        nbCalculationsBetweenVelocityBonusFactorChange = (int)(
            velocityBonusFactorChangePeriod / (Time.fixedDeltaTime * calculationInterval)
        );

        SkinnedMeshRenderer skinnedMeshRenderer = prefab.
            GetComponentInChildren<SkinnedMeshRenderer>();
        hasRig = skinnedMeshRenderer != null;

        if (hasRig)
        {
            Transform[] bones = skinnedMeshRenderer.bones;

            boneBaseDistanceToHead = new float[bones.Length];
            boneBaseRotation = new Quaternion[bones.Length];
            Vector3 headPosition = bones[0].position;

            for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
            {
                boneBaseDistanceToHead[boneIndex] = Vector3.Distance(headPosition, bones[boneIndex].position);
                boneBaseRotation[boneIndex] = bones[boneIndex].rotation;
            }

            float minVelocity = velocities.Values.ToArray().Min();
            nbTransformsToStore = (int)Mathf.Ceil(
                boneBaseDistanceToHead[bones.Length - 1] * (1 + scaleVariations) /
                (minVelocityBonusFactor * minVelocity * Time.fixedDeltaTime)
            );
        }
    }
}

[System.Serializable]
public class BoidsParameters : EntityParameters
{
    [Range(0, 10), Tooltip("Tendency to distance itself from others")]
    public float separationBaseWeight;
    [Range(0, 10), Tooltip("Tendency to align its direction with those of its neighbors")]
    public float alignmentBaseWeight;
    [Range(0, 10), Tooltip("Tendency to try to get closer to the others")]
    public float cohesionBaseWeight;
    [Range(0, 10), Tooltip("Tendency to distance itself from predators")]
    public float fearBaseWeight;

    [Space, Range(0, 15), Tooltip("Distance below which the boid will try to distance itself from others")]
    public float separationRadius;
    [Range(0, 15), Tooltip("Distance above which the boid will try to get closer to the others")]
    public float cohesionRadius;
    [Range(0, 15), Tooltip("Distance under which the boid will try to distance itself from the predator")]
    public float fearRadius;
    [HideInInspector]
    public float squaredSeparationRadius;
    [HideInInspector]
    public float squaredCohesionRadius;
    [HideInInspector]
    public float squaredFearRadius;
    [HideInInspector]
    public float squaredFullSeparationRadius;
    [HideInInspector]
    public float squaredFullCohesionRadius;
    [HideInInspector]
    public float squaredFullFearRadius;
    [HideInInspector]
    public float separationSmoothRangeSizeInverse;
    [HideInInspector]
    public float cohesionSmoothRangeSizeInverse;
    [HideInInspector]
    public float fearSmoothRangeSizeInverse;

    [Space, Range(0, 15), Tooltip("Used to adapt vision distance to improve performances")]
    public int idealNbNeighbors;

    [Space, Range(0, 10)]
    public int nbBoidsNearbyToBeAlone;

    [Space, Range(0, 50), Tooltip("In u/s")]
    public float normalVelocity;
    [Range(0, 50), Tooltip("In u/s")]
    public float aloneVelocity;
    [Range(0, 50), Tooltip("In u/s")]
    public float afraidVelocity;

    public override void PreCalculateParameters(int calculationInterval, float smoothnessRadiusOffset)
    {
        squaredSeparationRadius = MathHelpers.Square(separationRadius);
        squaredFullSeparationRadius = MathHelpers.Square(
            separationRadius - smoothnessRadiusOffset);
        separationSmoothRangeSizeInverse = 1 /
            (squaredFullSeparationRadius - squaredSeparationRadius);
        squaredCohesionRadius = MathHelpers.Square(cohesionRadius);
        squaredFullCohesionRadius = MathHelpers.Square(
            cohesionRadius + smoothnessRadiusOffset);
        cohesionSmoothRangeSizeInverse = 1 /
            (squaredFullCohesionRadius - squaredCohesionRadius);
        squaredFearRadius = MathHelpers.Square(fearRadius);
        squaredFullFearRadius = MathHelpers.Square(
            fearRadius - smoothnessRadiusOffset);
        fearSmoothRangeSizeInverse = 1 /
            (squaredFullFearRadius - squaredFearRadius);

        velocities = new Dictionary<State, float>{
            {State.NORMAL, normalVelocity},
            {State.ALONE, aloneVelocity},
            {State.AFRAID, afraidVelocity},
        };

        defaultState = State.NORMAL;

        animationFirstBone = 1;

        base.PreCalculateParameters(calculationInterval, smoothnessRadiusOffset);
    }
}

[System.Serializable]
public class PredatorsParameters : EntityParameters
{
    [Range(0, 10), Tooltip("How attracted the predator is to boids")]
    public float preyAttractionBaseWeight;
    [Range(0, 10), Tooltip("How repulsed the predator is to the other ones")]
    public float peerRepulsionBaseWeight;

    [Space, Range(0, 15), Tooltip("Distance under which the predator will try to distance itself from others")]
    public float peerRepulsionRadius;
    [HideInInspector]
    public float squaredPeerRepulsionRadius;
    [HideInInspector]
    public float squaredFullPeerRepulsionRadius;
    [HideInInspector]
    public float peerRepulsionSmoothRangeSizeInverse;

    [Space, Range(0, 15), Tooltip("In seconds")]
    public float averageChillingTime;
    [HideInInspector]
    public float probaHuntingAfterChilling;
    [Range(0, 15), Tooltip("In seconds")]
    public float averageHuntingTime;
    [HideInInspector]
    public float probaChillingAfterHunting;
    [Range(0, 1), Tooltip("Probability for the predator to enter HUNTING state again after exiting ATTACKING state")]
    public float probaHuntingAfterAttacking;
    [Range(0, 500), Tooltip("Number of prey in predator FOV needed to switch from HUNTING state to ATTACKING state")]
    public int nbPreysToAttack;

    [Space, Range(0, 50), Tooltip("In u/s")]
    public float chillingVelocity;
    [Range(0, 50), Tooltip("In u/s")]
    public float huntingVelocity;
    [Range(0, 50), Tooltip("In u/s")]
    public float attackingVelocity;

    [Space, Range(0, 3)]
    public float wavesBaseSpacialFrequency;
    [Range(0, 0.2f)]
    public float wavesBaseMagnitude;
    [Range(0, 0.2f)]
    public float wavesBaseSpeed;
    [Range(0, 5)]
    public float wavesEnveloppeMin;
    [Range(0, 5)]
    public float wavesEnveloppeGradient;
    [Range(0, 1)]
    public float velocityImpactOnWaves;

    public override void PreCalculateParameters(int calculationInterval, float smoothnessRadiusOffset)
    {
        squaredPeerRepulsionRadius = MathHelpers.Square(peerRepulsionRadius);
        squaredFullPeerRepulsionRadius = MathHelpers.Square(
            peerRepulsionRadius - smoothnessRadiusOffset);
        peerRepulsionSmoothRangeSizeInverse = 1 /
            (squaredFullPeerRepulsionRadius - squaredPeerRepulsionRadius);

        velocities = new Dictionary<State, float>{
            {State.CHILLING, chillingVelocity},
            {State.HUNTING, huntingVelocity},
            {State.ATTACKING, attackingVelocity}
        };

        defaultState = State.CHILLING;

        probaHuntingAfterChilling = ComputeStateChangeProba(
            averageChillingTime, calculationInterval);
        probaChillingAfterHunting = ComputeStateChangeProba(
            averageHuntingTime, calculationInterval);

        animationFirstBone = 0;

        base.PreCalculateParameters(calculationInterval, smoothnessRadiusOffset);
    }

    private float ComputeStateChangeProba(float averageTimeInState, int calculationInterval)
    {
        return (calculationInterval * Time.fixedDeltaTime) / averageTimeInState;
    }
}