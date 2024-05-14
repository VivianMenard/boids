using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract class to regroup all the parameters that define the behavior of a type of entity.
/// </summary>
[System.Serializable]
public abstract class EntityParameters
{
    [Tooltip("The prefab to use for this entity.")]
    public GameObject prefab;

    [Space, Range(0, 1), Tooltip("The higher this value is, the more size difference there will be among entities.")]
    public float scaleVariations;

    [Space, Tooltip("The spawnable area of this entity.")]
    public GameObject spawnAreaGO;
    [HideInInspector]
    public AreaScript spawnArea;

    [Space, Range(0, 30), Tooltip("The raycast distance used to detect obstacles.")]
    public float raycastBaseDistance;
    [Range(0, 5), Tooltip("The distance entities will try to keep between them and the obstacle.")]
    public float obstacleBaseMargin;
    [Tooltip("If true, all obstacle avoidance parameters will be proportional to entity velocity. Must not be switched during playmode.")]
    public bool applyVelocityFactor;
    [HideInInspector]
    public LayerMask obstaclesLayerMask;

    [Space, Range(0, 50), Tooltip("In u/s², the maximum acceleration of the entity in a normal situation.")]
    public float acceleration;
    [Range(0, 50), Tooltip("In u/s², the maximum acceleration of the entity in an emergency situation.")]
    public float emergencyAcceleration;
    [Range(0, 10), Tooltip("In seconds, random speed bonus change period.")]
    public float velocityBonusFactorChangePeriod;
    [HideInInspector]
    public int nbCalculationsBetweenVelocityBonusFactorChange;
    [Range(0, 1), Tooltip("Minimum random multiplicative speed bonus.")]
    public float minVelocityBonusFactor;
    [Range(1, 2), Tooltip("Maximum random multiplicative speed bonus.")]
    public float maxVelocityBonusFactor;


    [Space, Range(0, 50), Tooltip("In random walk: Number of fixed updates between state changes.")]
    public int rwStatePeriod;
    [Range(0, 5), Tooltip("In random walk: Tendency to choose a new direction close to the old one.")]
    public float rwMomentumWeight;
    [Range(0, 1), Tooltip("In random walk: Probability to go straight.")]
    public float rwProbaStraightLine;
    [Range(0, 1), Tooltip("Mainly for random walk but also used for spawn direction: the higher this value is, the more the entity will avoid vertical directions.")]
    public float rwVerticalRestriction;
    [Range(0, 10), Tooltip("In random walk: Max number of attempts to find a new random direction without obstacles.")]
    public int rwMaxAttempts;

    [Space, Range(0, 15), Tooltip("In u, the maximum distance at which the entity can see another one.")]
    public int visionDistance;
    [Range(0, 180), Tooltip("In degrees, half of the vision field of the entity.")]
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
    public float[] boneBaseDistanceToHead;
    [HideInInspector]
    public int nbTransformsToStore;
    [HideInInspector]
    public Quaternion[] boneBaseRotation;
    [HideInInspector]
    public int animationFirstBone;

    [HideInInspector]
    public float[] avoidanceDirectionPreferences;

    /// <summary>
    /// Pre-calculates all the parameters of the entity that require it.
    /// </summary>
    /// 
    /// <param name="calculationInterval">The number of fixed updates between every entity behavior update.</param>
    /// <param name="smoothnessRadiusOffset">The size of the gradient area between to behaviors in u, used by the system to smooth behaviors.</param>
    /// <param name="nbBoids">The number of boids in the simulation.</param>
    public virtual void PreCalculateParameters(
        int calculationInterval, float smoothnessRadiusOffset, int nbBoids
    )
    {
        spawnArea = spawnAreaGO.GetComponent<AreaScript>();
        if (spawnArea == null)
            throw new MissingComponentException(
                "No AreaScript component on spawnAreaGO GameObject."
            );

        cosVisionSemiAngle = Mathf.Cos(visionSemiAngle * Mathf.Deg2Rad);

        nbCalculationsBetweenVelocityBonusFactorChange = (int)(
            velocityBonusFactorChangePeriod / (Time.fixedDeltaTime * calculationInterval)
        );

        SkinnedMeshRenderer skinnedMeshRenderer = prefab.
            GetComponentInChildren<SkinnedMeshRenderer>();

        if (skinnedMeshRenderer == null)
            throw new MissingComponentException("The entity has no rig.");

        Transform[] bones = skinnedMeshRenderer.bones;

        boneBaseDistanceToHead = new float[bones.Length];
        boneBaseRotation = new Quaternion[bones.Length];
        Vector3 headPosition = bones[0].position;

        for (int boneIndex = 0; boneIndex < bones.Length; boneIndex++)
        {
            boneBaseDistanceToHead[boneIndex] = Vector3.Distance(
                headPosition,
                bones[boneIndex].position
            );
            boneBaseRotation[boneIndex] = bones[boneIndex].rotation;
        }

        float minVelocity = velocities.Values.ToArray().Min();
        nbTransformsToStore = (int)Mathf.Ceil(
            boneBaseDistanceToHead[bones.Length - 1] * (1 + scaleVariations) /
            (minVelocityBonusFactor * minVelocity * Time.fixedDeltaTime)
        );
    }
}

/// <summary>
/// Regroups all the parameters that define the behavior of the boids.
/// </summary>
[System.Serializable]
public class BoidsParameters : EntityParameters
{
    [Range(0, 10), Tooltip("Tendency to distance itself from others.")]
    public float separationBaseWeight;
    [Range(0, 10), Tooltip("Tendency to align its direction with those of its neighbors.")]
    public float alignmentBaseWeight;
    [Range(0, 10), Tooltip("Tendency to try to get closer to the others.")]
    public float cohesionBaseWeight;
    [Range(0, 10), Tooltip("Tendency to distance itself from predators.")]
    public float fearBaseWeight;

    [Space, Range(0, 15), Tooltip("In u, distance below which the boid will try to distance itself from others.")]
    public float separationRadius;
    [Range(0, 15), Tooltip("In u, distance above which the boid will try to get closer to the others.")]
    public float cohesionRadius;
    [Range(0, 15), Tooltip("In u, distance under which the boid will try to distance itself from the predator.")]
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

    [Space, Range(0, 15), Tooltip("Number of Neighbors that the boid will try to have in its field of view by changing its vision distance.")]
    public int idealNbNeighbors;

    [Space, Range(0, 10), Tooltip("Number of boids nearby to adopt ALONE state.")]
    public int nbBoidsNearbyToBeAlone;

    [Space, Range(0, 50), Tooltip("In u/s, velocity of the boid in NORMAL state.")]
    public float normalVelocity;
    [Range(0, 50), Tooltip("In u/s, velocity of the boid in ALONE state.")]
    public float aloneVelocity;
    [Range(0, 50), Tooltip("In u/s, velocity of the boid in AFRAID state.")]
    public float afraidVelocity;

    public override void PreCalculateParameters(
        int calculationInterval, float smoothnessRadiusOffset, int nbBoids
    )
    {
        squaredSeparationRadius = MathHelpers.Square(separationRadius);
        squaredFullSeparationRadius = MathHelpers.Square(
            separationRadius - smoothnessRadiusOffset);
        squaredCohesionRadius = MathHelpers.Square(cohesionRadius);
        squaredFullCohesionRadius = MathHelpers.Square(
            cohesionRadius + smoothnessRadiusOffset);
        squaredFearRadius = MathHelpers.Square(fearRadius);
        squaredFullFearRadius = MathHelpers.Square(
            fearRadius - smoothnessRadiusOffset);

        velocities = new Dictionary<State, float>{
            {State.NORMAL, normalVelocity},
            {State.ALONE, aloneVelocity},
            {State.AFRAID, afraidVelocity},
        };

        defaultState = State.NORMAL;

        animationFirstBone = 1;

        avoidanceDirectionPreferences = new float[8] { 1, 1, 1, 1, 1, 1, 1, 1 };

        obstaclesLayerMask = LayerMask.GetMask(
            Constants.obstaclesLayerName,
            Constants.boidsObstaclesLayerName
        );

        base.PreCalculateParameters(calculationInterval, smoothnessRadiusOffset, nbBoids);
    }
}

/// <summary>
/// Regroups all the parameters that define the behavior of the predators.
/// </summary>
[System.Serializable]
public class PredatorsParameters : EntityParameters
{
    [Range(0, 10), Tooltip("How attracted the predator is to boids.")]
    public float preyAttractionBaseWeight;
    [Range(0, 10), Tooltip("How repulsed the predator is to the other ones.")]
    public float peerRepulsionBaseWeight;

    [Space, Range(0, 1), Tooltip("The higher this value is, the more the predator will avoid vertical directions.")]
    public float verticalRestriction;
    [Range(0, 1), Tooltip("The higher this value is, the more the predator will favor horizontal directions to avoid obstacles.")]
    public float preferenceForHorizontalAvoidance;

    [Space, Range(0, 15), Tooltip("In u, distance under which the predator will try to distance itself from others.")]
    public float peerRepulsionRadius;
    [HideInInspector]
    public float squaredPeerRepulsionRadius;
    [HideInInspector]
    public float squaredFullPeerRepulsionRadius;

    [Space, Range(0, 15), Tooltip("In seconds, average time that a predator will spend in CHILLING state before changing state.")]
    public float averageChillingTime;
    [HideInInspector]
    public float probaHuntingAfterChilling;
    [Range(0, 15), Tooltip("In seconds, average time a predator will spend in HUNTING state before chilling again.")]
    public float averageHuntingTime;
    [HideInInspector]
    public float probaChillingAfterHunting;
    [Range(0, 1), Tooltip("Probability for the predator to enter HUNTING state again after exiting ATTACKING state.")]
    public float probaHuntingAfterAttacking;
    [Range(0, 1), Tooltip("Percentage of preys in predator FOV needed to switch from HUNTING state to ATTACKING state.")]
    public float percentagePreysToAttack;
    [HideInInspector]
    public int nbPreysToAttack;

    [Space, Range(0, 50), Tooltip("In u/s, velocity of the predator in CHILLING state.")]
    public float chillingVelocity;
    [Range(0, 50), Tooltip("In u/s, velocity of the predator in HUNTING state.")]
    public float huntingVelocity;
    [Range(0, 50), Tooltip("In u/s, velocity of the predator in ATTACKING state.")]
    public float attackingVelocity;

    [Space, Range(0, 3), Tooltip("In u^(-1), spacial frequency of the wave animation on predators, when at CHILLING velocity.")]
    public float wavesBaseSpacialFrequency;
    [Range(0, 0.2f), Tooltip("In u, magnitude of the wave animation on predators, when at CHILLING velocity.")]
    public float wavesBaseMagnitude;
    [Range(0, 0.2f), Tooltip("In u/s, speed of the wave animation on predators, when at CHILLING velocity.")]
    public float wavesBaseSpeed;
    [Range(0, 5), Tooltip("In u, magnitude of the wave animation at the head of the predators, when at CHILLING velocity.")]
    public float wavesEnveloppeMin;
    [Range(0, 5), Tooltip("How much the magnitude of the animation increases with distance from head of the predator.")]
    public float wavesEnveloppeGradient;
    [Range(0, 1), Tooltip("How much the velocity of the predator will impact animation parameters.")]
    public float velocityImpactOnWaves;

    public override void PreCalculateParameters(
        int calculationInterval, float smoothnessRadiusOffset, int nbBoids
    )
    {
        squaredPeerRepulsionRadius = MathHelpers.Square(peerRepulsionRadius);
        squaredFullPeerRepulsionRadius = MathHelpers.Square(
            peerRepulsionRadius - smoothnessRadiusOffset);

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

        float horizontalPreference = 1 + preferenceForHorizontalAvoidance,
            semiHorizontalPreference = 1 + 0.5f * preferenceForHorizontalAvoidance,
            verticalPreference = 1;

        avoidanceDirectionPreferences = new float[8] {
            horizontalPreference, // right preference
            horizontalPreference, // left preference
            semiHorizontalPreference, // down right preference
            semiHorizontalPreference, // up right preference
            semiHorizontalPreference, // down left preference
            semiHorizontalPreference, // up left preference
            verticalPreference, // down preference
            verticalPreference // up preference
        };

        UpdateNbPreysToAttack(nbBoids);

        obstaclesLayerMask = LayerMask.GetMask(
            Constants.obstaclesLayerName,
            Constants.predatorsObstaclesLayerName
        );

        base.PreCalculateParameters(calculationInterval, smoothnessRadiusOffset, nbBoids);
    }

    /// <summary>
    /// Pre-calculate the number of boids in the fov of a predator required for it to attack, 
    /// based of the percentage required and the total number of boids.
    /// </summary>
    /// 
    /// <param name="nbBoids">The number of boids in the simulation.</param>
    public void UpdateNbPreysToAttack(int nbBoids)
    {
        nbPreysToAttack = (int)Mathf.Max(percentagePreysToAttack * nbBoids, 10);
    }

    /// <summary>
    /// Pre-calculate the probability to leave a state at each calculation, 
    /// base on the computation interval, and on the average time that the state must last. 
    /// </summary>
    /// 
    /// <param name="averageTimeInState">The average time the state must last.</param>
    /// <param name="calculationInterval">The number of fixed updates between every entity behavior update.</param>
    private float ComputeStateChangeProba(float averageTimeInState, int calculationInterval)
    {
        return (calculationInterval * Time.fixedDeltaTime) / averageTimeInState;
    }
}

/// <summary>
/// Regroups all the global parameters shared by all shader graphs.
/// </summary>
[System.Serializable]
public class SharedParameters
{
    [Range(0, 0.4f), Tooltip("In u^(-1), the lowest spacial frequency among water movement contributions.")]
    public float baseSpacialFrequency;
    [Range(0, 5), Tooltip("In u/s, the lowest speed among water movement contributions.")]
    public float baseSpeed;
    [Range(0, 5), Tooltip("In u, the highest amplitude among water movement contributions.")]
    public float baseAmplitude;
    [Range(1, 1.3f), Tooltip("The factor between the spacial frequency of a water movement contribution and the one the next contribution.")]
    public float spacialFrequencyFactor;
    [Range(0, 2), Tooltip("The factor between the speed of a water movement contribution and the one the next contribution.")]
    public float speedFactor;
    [Range(0, 1), Tooltip("The factor between the amplitude of a water movement contribution and the one the next contribution.")]
    public float amplitudeFactor;
    [Range(0, 5), Tooltip("The strengh of the fictive lateral movements in water.")]
    public float lateralMovementsStrengh;

    [Space, Range(0, 150), Tooltip("The traveled distance in water from which absportion and fog effects will be maximal.")]
    public float maxDistance;
    [Tooltip("The color of the water distance fog.")]
    public Color fogColor;
    [Range(0, 1), Tooltip("The strengh of the water distance fog.")]
    public float fogStrengh;
    [Tooltip("The complementary color of the one that the water absporb.")]
    public Color absportionComplementaryColor;
    [Range(0, 1), Tooltip("The strengh of the water absportion effect.")]
    public float absorptionStrengh;
    [Range(0, 1), Tooltip("The base depth indicator (depth indicator for no traveled distance in water).")]
    public float minDepth;

    [Space, Range(0, 1), Tooltip("The intensity of the caustics effect.")]
    public float causticsIntensity;
    [Tooltip("The direction from which the caustics are projected.")]
    public Vector3 causticsProjectionDirection;
    [Tooltip("The color of the caustics.")]
    public Color causticsColor;
    [Range(0, 5), Tooltip("The cell density in caustics voronoi layers.")]
    public float causticsCellsDensity;
    [Range(0, 10), Tooltip("The speed of movements in caustics layers.")]
    public float causticsSpeed;
    [Range(0, 50), Tooltip("The tiling of the noise that deforms caustics layers.")]
    public float causticsNoiseTiling;
    [Range(0, 5), Tooltip("The strengh of the noise that deforms caustics layers.")]
    public float causticsNoiseStrengh;
    [Range(0, 5), Tooltip("The paning speed of the noise that deforms caustics layers.")]
    public float causticsNoiseSpeed;
    [Tooltip("The offset between the two caustics layers.")]
    public Vector2 causticsLayerOffset;
    [Range(0, 2), Tooltip("The cells density of the noise that varies caustics effect intensity.")]
    public float causticsIntensityNoiseCellsDensity;
    [Range(0, 5), Tooltip("The speed of the movements in the noise that varies caustics effect intensity.")]
    public float causticsIntensityNoiseSpeed;
    [Range(0, 1), Tooltip("The strengh of the noise that varies caustics effect intensity.")]
    public float causticsIntensityNoiseStrengh;
    [Range(0, 1), Tooltip("The strengh of the additional texture that varies caustics effect intensity.")]
    public float causticsAdditionalTextureStrengh;
    [Range(50, 100), Tooltip("The tiling of the additional texture that varies caustics effect intensity.")]
    public float causticsAdditionalTextureTiling;

    [Space, Range(0, 100), Tooltip("The tiling of the foam texture of the water.")]
    public float foamTextureTiling;
}