using System.Collections.Generic;
using UnityEngine;

public class EntitiesManagerScript : MonoBehaviour
{
    [SerializeField, Range(0, 3)]
    private float timeScale;

    [SerializeField, Range(1, 15), Space, Tooltip("Number of FixedUpdates between velocity calculations")]
    private int calculationInterval;
    public int CalculationInterval { get { return calculationInterval; } }

    [SerializeField, Range(0, 1), Tooltip("Size of the smoothing zone between two behaviors")]
    private float smoothnessRadiusOffset;

    [Space, SerializeField, Range(0, 3000)]
    private int numberOfBoids;
    [SerializeField, Range(0, 10)]
    private int numberOfPredators;

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

    private int currentNbBoids = 0, currentNbPredators = 0;

    [Space]
    public BoidsParameters boidsParams;

    [Space]
    public PredatorsParameters predatorsParams;

    private long clock = 0;
    public long Clock { get { return clock; } }

    [HideInInspector]
    public bool EntitiesMovement = true;

    [HideInInspector]
    public LayerMask ObstacleLayerMask, EntitiesLayerMask;

    [HideInInspector]
    public Dictionary<State, bool> IsItEmergencyState = new Dictionary<State, bool>{
        {State.NORMAL, false},
        {State.ALONE, false},
        {State.AFRAID, true},
        {State.CHILLING, false},
        {State.HUNTING, false},
        {State.ATTACKING, true}
    };

    [HideInInspector]
    public Dictionary<int, float> VisionDistanceSmoothRangeSizeInverses = new Dictionary<int, float>();

    private List<GameObject> boids = new List<GameObject>(),
        predators = new List<GameObject>();

    private Dictionary<Collider, EntityType> colliderToEntityType = new Dictionary<Collider, EntityType>();
    private Dictionary<Collider, Vector3> colliderToDirection = new Dictionary<Collider, Vector3>();
    private Dictionary<Collider, Vector3> colliderToPosition = new Dictionary<Collider, Vector3>();

    public EntityType ColliderToEntityType(Collider collider)
    {
        return colliderToEntityType[collider];
    }

    public Vector3 ColliderToDirection(Collider collider)
    {
        return colliderToDirection[collider];
    }

    public Vector3 ColliderToPosition(Collider collider)
    {
        return colliderToPosition[collider];
    }

    public void UpdateEntityPosition(Collider collider, Vector3 newPosition)
    {
        colliderToPosition[collider] = newPosition;
    }

    public void UpdateEntityDirection(Collider collider, Vector3 newDirection)
    {
        colliderToDirection[collider] = newDirection;
    }

    public void RemoveAssociatedEntries(Collider collider)
    {
        colliderToEntityType.Remove(collider);
        colliderToDirection.Remove(collider);
        colliderToPosition.Remove(collider);
    }

    private void Awake()
    {
        PreCalculateParameters();
        AdjustPredatorsColliders();
    }

    void Start()
    {
        ObstacleLayerMask = LayerMask.GetMask(Constants.obstaclesLayerName);
        EntitiesLayerMask = LayerMask.GetMask(
            Constants.boidsLayerName,
            Constants.predatorsLayerName
        );
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
            VisionDistanceSmoothRangeSizeInverses[visionDistance] = 1 /
                (MathHelpers.Square(visionDistance - smoothnessRadiusOffset) -
                MathHelpers.Square(visionDistance));
        }
    }

    private void FixedUpdate()
    {
        if (EntitiesMovement)
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
            Vector3 entityDirection = MathHelpers.
                GetRandomDirection(restrictVerticaly: true);
            Quaternion entityRotation = Quaternion.LookRotation(entityDirection);
            Vector3 entityPosition = GetRandomSpawnablePositionInArea(type);

            GameObject entity = Instantiate(
                entityPrefab,
                entityPosition,
                entityRotation,
                gameObject.transform
            );

            Collider entityCollider = entity.GetComponent<Collider>();
            if (entityCollider == null)
                throw new MissingComponentException(
                    "No Collider component on Entity."
                );

            colliderToEntityType[entityCollider] = type;
            colliderToDirection[entityCollider] = entityDirection;
            colliderToPosition[entityCollider] = entityPosition;

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
        AreaScript spawnArea = (type == EntityType.BOID) ?
            boidsParams.spawnArea : predatorsParams.spawnArea;

        return new Vector3(
            UnityEngine.Random.Range(spawnArea.MinPt.x, spawnArea.MaxPt.x),
            UnityEngine.Random.Range(spawnArea.MinPt.y, spawnArea.MaxPt.y),
            UnityEngine.Random.Range(spawnArea.MinPt.z, spawnArea.MaxPt.z)
        );
    }

    private void OnValidate()
    {
        Time.timeScale = timeScale;

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
            if (sphereCollider == null)
                throw new MissingComponentException(
                    "No sphereCollider component on predator GameObject."
                );

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
