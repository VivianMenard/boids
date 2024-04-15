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

    [Space, SerializeField, Range(0, 3000)]
    private int numberOfBoids;
    [SerializeField, Range(0, 10)]
    private int numberOfPredators;

    private int currentNbBoids = 0, currentNbPredators = 0;

    [HideInInspector]
    public LayerMask obstacleLayerMask, entitiesLayerMask;
    [HideInInspector]
    public int boidsLayer, predatorsLayer;

    [HideInInspector]
    public Dictionary<State, bool> isItEmergencyState = new Dictionary<State, bool>{
        {State.NORMAL, false},
        {State.ALONE, false},
        {State.AFRAID, true},
        {State.CHILLING, false},
        {State.HUNTING, false},
        {State.ATTACKING, true}
    };

    [HideInInspector]
    public Dictionary<int, float> visionDistanceSmoothRangeSizeInverses = new Dictionary<int, float>();

    private AreaScript area;

    private List<GameObject> boids = new List<GameObject>(),
        predators = new List<GameObject>();

    [Space]
    public BoidsParameters boidsParams;

    [Space]
    public PredatorsParameters predatorsParams;

    [HideInInspector]
    public bool entitiesMovement = true;

    public int NumberOfBoids
    {
        get { return numberOfBoids; }
        set
        {
            numberOfBoids = value;
            predatorsParams.UpdateNbPreysToAttack(value);
        }
    }

    public int NumberOfPredators
    {
        get { return numberOfPredators; }
        set { numberOfPredators = value; }
    }

    private void Awake()
    {
        PreCalculateParameters();
        AdjustPredatorsColliders();
    }

    void Start()
    {
        area = GameObject.FindGameObjectWithTag(Constants.areaTag).
            GetComponent<AreaScript>();
        obstacleLayerMask = LayerMask.GetMask(Constants.obstaclesLayerName);
        entitiesLayerMask = LayerMask.GetMask(
            Constants.boidsLayerName,
            Constants.predatorsLayerName
        );
        boidsLayer = LayerMask.NameToLayer(Constants.boidsLayerName);
        predatorsLayer = LayerMask.NameToLayer(Constants.predatorsLayerName);
    }

    private void PreCalculateParameters()
    {
        boidsParams.PreCalculateParameters(
            calculationInterval, smoothnessRadiusOffset, numberOfBoids
        );
        predatorsParams.PreCalculateParameters(
            calculationInterval, smoothnessRadiusOffset, numberOfBoids
        );

        for (
            int visionDistance = 1;
            visionDistance <= Mathf.Max(
                boidsParams.visionDistance, predatorsParams.visionDistance
            );
            visionDistance++
        )
        {
            visionDistanceSmoothRangeSizeInverses[visionDistance] = 1 /
                (MathHelpers.Square(visionDistance - smoothnessRadiusOffset) -
                MathHelpers.Square(visionDistance));
        }
    }

    private void FixedUpdate()
    {
        if (entitiesMovement)
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
            Quaternion entityRotation = Quaternion.LookRotation(
                MathHelpers.GetRandomDirection(restrictVerticaly: true)
            );
            GameObject entity = Instantiate(
                entityPrefab,
                GetRandomSpawnablePositionInArea(type),
                entityRotation,
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
        float scaleVariations = (type == EntityType.BOID) ?
            boidsParams.scaleVariations : predatorsParams.scaleVariations;

        entity.transform.localScale *= (1 + scaleVariations * Random.Range(-1, 1));
    }

    private Vector3 GetRandomSpawnablePositionInArea(EntityType type)
    {
        float obstacleMargin = (type == EntityType.BOID) ?
            boidsParams.spawnMargin : predatorsParams.spawnMargin;

        Vector3 minPtToSpawn = area.minPt + obstacleMargin * Vector3.one;
        Vector3 maxPtToSpawn = area.maxPt - obstacleMargin * Vector3.one;

        return new Vector3(
            UnityEngine.Random.Range(minPtToSpawn.x, maxPtToSpawn.x),
            UnityEngine.Random.Range(minPtToSpawn.y, maxPtToSpawn.y),
            UnityEngine.Random.Range(minPtToSpawn.z, maxPtToSpawn.z)
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
