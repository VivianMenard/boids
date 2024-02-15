using System.Collections.Generic;
using UnityEngine;

public class EntitiesManagerScript : MonoBehaviour
{
    [Range(0, 3)]
    public float TimeScale;

    [Range(1, 15), Space, Tooltip("Number of FixedUpdates between velocity calculations")]
    public int calculationInterval;
    [Range(0, 1), Tooltip("Size of the smoothing zone between two behaviors")]
    public float smoothnessRadiusOffset;

    [HideInInspector]
    public long clock = 0;

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

    [HideInInspector]
    public Dictionary<int, float> visionDistanceSmoothRangeSizeInverses = new Dictionary<int, float>();

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
        boidsParams.PreCalculateParameters(calculationInterval, smoothnessRadiusOffset);
        predatorsParams.PreCalculateParameters(calculationInterval, smoothnessRadiusOffset);

        for (
            int visionDistance = 1;
            visionDistance <= Mathf.Max(boidsParams.visionDistance, predatorsParams.visionDistance);
            visionDistance++
        )
        {
            visionDistanceSmoothRangeSizeInverses[visionDistance] = 1 /
                (MathHelpers.Square(visionDistance - smoothnessRadiusOffset) - MathHelpers.Square(visionDistance));
        }
    }

    private void FixedUpdate()
    {
        clock++;

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
