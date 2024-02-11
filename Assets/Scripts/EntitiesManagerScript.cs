using System.Collections.Generic;
using UnityEngine;

public class EntitiesManagerScript : MonoBehaviour
{
    [Range(0, 3)]
    public float TimeScale;

    [Range(1, 15), Space, Tooltip("Number of FixedUpdates between velocity calculations")]
    public int calculationInterval;
    [Range(0, 3), Tooltip("Size of the smoothing zone between two behaviors")]
    public float smoothnessRadiusOffset;

    [HideInInspector]
    public int clock = 0;

    [Space, Range(0, 3000), SerializeField]
    private int numberOfBoids;
    private int currentNbBoids = 0;

    [Range(0, 10), SerializeField]
    private int numberOfPredators;
    private int currentNbPredators = 0;

    [Space]
    public bool ObstaclesAvoidance;
    [Range(0, 15)]
    public float raycastDistance;
    [Range(0, 5), Tooltip("The distance entities will try to keep between them and the obstacle")]
    public float obstacleMargin;
    [HideInInspector]
    public LayerMask obstacleLayerMask;

    [HideInInspector]
    public LayerMask entitiesLayerMask;

    private AreaScript area;

    private List<GameObject> boids = new List<GameObject>();
    private List<GameObject> predators = new List<GameObject>();

    [Space]
    public BoidsParameters boidsParams;

    [Space]
    public PredatorsParameters predatorsParams;

    private enum EntityType
    {
        BOID,
        PREDATOR
    }

    void Start()
    {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();
        obstacleLayerMask = LayerMask.GetMask("Obstacles");
        entitiesLayerMask = LayerMask.GetMask("Boids", "Predators");
    }

    private void PreCalculateParameters()
    {
        boidsParams.squaredSeparationRadius = Square(
            boidsParams.separationRadius);
        boidsParams.squaredFullSeparationRadius = Square(
            boidsParams.separationRadius - smoothnessRadiusOffset);
        boidsParams.squaredCohesionRadius = Square(
            boidsParams.cohesionRadius);
        boidsParams.squaredFullCohesionRadius = Square(
            boidsParams.cohesionRadius + smoothnessRadiusOffset);
        boidsParams.squaredFearRadius = Square(boidsParams.fearRadius);
        boidsParams.squaredFullFearRadius = Square(
            boidsParams.fearRadius - smoothnessRadiusOffset);
        predatorsParams.squaredPeerRepulsionRadius = Square(
            predatorsParams.peerRepulsionRadius);
        predatorsParams.squaredFullPeerRepulsionRadius = Square(
            predatorsParams.peerRepulsionRadius - smoothnessRadiusOffset);

        boidsParams.cosVisionSemiAngle = Mathf.Cos(
            boidsParams.visionSemiAngle * Mathf.Deg2Rad);
        predatorsParams.cosVisionSemiAngle = Mathf.Cos(
            predatorsParams.visionSemiAngle * Mathf.Deg2Rad);

        boidsParams.velocities = new Dictionary<State, float>{
            {State.NORMAL, boidsParams.normalVelocity},
            {State.ALONE, boidsParams.aloneVelocity},
            {State.AFRAID, boidsParams.afraidVelocity},
        };
        boidsParams.referenceVelocity = boidsParams.normalVelocity;

        predatorsParams.velocities = new Dictionary<State, float>{
            {State.CHILLING, predatorsParams.chillingVelocity},
            {State.HUNTING, predatorsParams.huntingVelocity},
            {State.ATTACKING, predatorsParams.attackingVelocity}
        };
        predatorsParams.referenceVelocity = predatorsParams.huntingVelocity;

        predatorsParams.probaHuntingAfterChilling = ComputeStateChangeProba(
            predatorsParams.averageChillingTime);
        predatorsParams.probaChillingAfterHunting = ComputeStateChangeProba(
            predatorsParams.averageHuntingTime);
    }

    private float Square(float value)
    {
        return value * value;
    }

    private float ComputeStateChangeProba(float averageTimeInState)
    {
        return (calculationInterval * Time.fixedDeltaTime) / averageTimeInState;
    }

    private void FixedUpdate()
    {
        clock = (clock + 1) % calculationInterval;

        AdjustNbEntities(
            numberOfBoids - currentNbBoids,
            EntityType.BOID
        );
        AdjustNbEntities(
            numberOfPredators - currentNbPredators,
            EntityType.PREDATOR
        );
    }

    private void AdjustNbEntities(int nbToSpawn, EntityType type)
    {
        if (nbToSpawn == 0)
            return;

        if (nbToSpawn > 0)
            SpawnEntities(nbToSpawn, type);
        else
            DespawnEntities(-nbToSpawn, type);
    }

    private void SpawnEntities(int nbToSpawn, EntityType type)
    {
        GameObject entityPrefab = (type == EntityType.BOID) ?
            boidsParams.prefab : predatorsParams.prefab;
        List<GameObject> entitiesList = (type == EntityType.BOID) ?
            boids : predators;

        for (int _ = 0; _ < nbToSpawn; _++)
        {
            GameObject entity = Instantiate(
                entityPrefab,
                GetRandomPositionInArea(),
                Quaternion.identity,
                gameObject.transform
            );
            SetEntityScale(entity, type);
            entitiesList.Add(entity);
        }

        IncrCurrentNbEntities(nbToSpawn, type);
    }

    private void DespawnEntities(int nbToDespanw, EntityType type)
    {
        int startIndex = currentNbBoids - 1;
        int endIndex = numberOfBoids;
        List<GameObject> entitiesList = boids;

        if (type == EntityType.PREDATOR)
        {
            startIndex = currentNbPredators - 1;
            endIndex = numberOfPredators;
            entitiesList = predators;
        }

        for (int index = startIndex; index >= endIndex; index--)
        {
            GameObject entity = entitiesList[index];
            entitiesList.RemoveAt(index);
            Destroy(entity);
        }

        IncrCurrentNbEntities(-nbToDespanw, type);
    }

    private void IncrCurrentNbEntities(int increment, EntityType type)
    {
        switch (type)
        {
            case EntityType.BOID:
                currentNbBoids += increment;
                break;
            case EntityType.PREDATOR:
                currentNbPredators += increment;
                break;
        }
    }

    private void SetEntityScale(GameObject entity, EntityType type)
    {
        float minScale = (type == EntityType.BOID) ?
            boidsParams.minScale : predatorsParams.minScale;
        float maxScale = (type == EntityType.BOID) ?
            boidsParams.maxScale : predatorsParams.maxScale;

        entity.transform.localScale *= Random.Range(minScale, maxScale);
    }

    private Vector3 GetRandomPositionInArea()
    {
        return new Vector3(
            UnityEngine.Random.Range(area.minPt.x, area.maxPt.x),
            UnityEngine.Random.Range(area.minPt.y, area.maxPt.y),
            UnityEngine.Random.Range(area.minPt.z, area.maxPt.z)
        );
    }

    private void OnValidate()
    {
        Time.timeScale = TimeScale;

        CheckBoundsForDynamicRangeParameters();
        PreCalculateParameters();
        AdjustPredatorsColliders();
    }

    private void AdjustPredatorsColliders()
    {
        void AdjustOnePredatorCollider(GameObject predator)
        {
            SphereCollider sphereCollider = predator
                .GetComponent<SphereCollider>();
            sphereCollider.radius = boidsParams.fearRadius;
        }

        AdjustOnePredatorCollider(predatorsParams.prefab);

        foreach (GameObject predator in predators)
            AdjustOnePredatorCollider(predator);
    }

    private void CheckBoundsForDynamicRangeParameters()
    {
        if (boidsParams.cohesionRadius < boidsParams.separationRadius)
            boidsParams.cohesionRadius = boidsParams.separationRadius;
    }
}
