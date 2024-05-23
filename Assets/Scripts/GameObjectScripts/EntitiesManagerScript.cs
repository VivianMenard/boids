using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all information and actions that concern all the entities.
/// </summary>
public class EntitiesManagerScript : MonoBehaviour
{
    [SerializeField, Range(0, 3), Tooltip("Time scale of the simulation.")]
    private float timeScale;

    [SerializeField, Range(1, 15), Space, Tooltip("The number of fixed updates between every entity behavior update.")]
    private int calculationInterval;
    public int CalculationInterval { get { return calculationInterval; } }

    [SerializeField, Range(0, 2), Tooltip("Size of the smoothing zone between two behaviors.")]
    private float smoothnessRadiusOffset;

    [Space, SerializeField, Range(0, 3000), Tooltip("Number of boids in the simulation.")]
    private int numberOfBoids;
    [SerializeField, Range(0, 10), Tooltip("Number of predators in the simulation.")]
    private int numberOfPredators;

    [Space]
    public BoidsParameters boidsParams;

    [Space]
    public PredatorsParameters predatorsParams;

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

    private long clock = 0;
    public long Clock { get { return clock; } }

    [HideInInspector]
    public bool EntitiesMovement = true;

    [HideInInspector]
    public LayerMask EntitiesLayerMask;

    [HideInInspector]
    public Dictionary<State, bool> IsItEmergencyState = new Dictionary<State, bool>{
        {State.NORMAL, false},
        {State.ALONE, false},
        {State.AFRAID, true},
        {State.CHILLING, false},
        {State.HUNTING, false},
        {State.ATTACKING, true}
    };

    private List<GameObject> boids = new List<GameObject>(),
        predators = new List<GameObject>();

    private Dictionary<Collider, EntityType> colliderToEntityType = new Dictionary<Collider, EntityType>();
    private Dictionary<Collider, Vector3> colliderToDirection = new Dictionary<Collider, Vector3>();
    private Dictionary<Collider, Vector3> colliderToPosition = new Dictionary<Collider, Vector3>();

    /// <summary>Allows to access entity type from its collider.</summary>
    /// <param name="collider">The entity collider.</param>
    /// <returns>The entity type.</returns>
    public EntityType ColliderToEntityType(Collider collider)
    {
        return colliderToEntityType[collider];
    }

    /// <summary>Allows to access entity direction from its collider.</summary>
    /// <param name="collider">The entity collider.</param>
    /// <returns>The entity direction.</returns>
    public Vector3 ColliderToDirection(Collider collider)
    {
        return colliderToDirection[collider];
    }

    /// <summary>Allows to access entity position from its collider.</summary>
    /// <param name="collider">The entity collider.</param>
    /// <returns>The entity position.</returns>
    public Vector3 ColliderToPosition(Collider collider)
    {
        return colliderToPosition[collider];
    }

    /// <summary>
    /// Updates the stored position of the entity.
    /// </summary>
    /// <param name="collider">The entity collider.</param>
    /// <param name="newPosition">The new position to store.</param>
    public void UpdateEntityPosition(Collider collider, Vector3 newPosition)
    {
        colliderToPosition[collider] = newPosition;
    }

    /// <summary>
    /// Updates the stored direction of the entity.
    /// </summary>
    /// <param name="collider">The entity collider.</param>
    /// <param name="newDirection">The new direction to store.</param>
    public void UpdateEntityDirection(Collider collider, Vector3 newDirection)
    {
        colliderToDirection[collider] = newDirection;
    }

    /// <summary>
    /// Removes all the stored information related to an entity.
    /// Used before entity deletion.
    /// </summary>
    /// <param name="collider">The entity collider.</param>
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
        EntitiesLayerMask = LayerMask.GetMask(
            Constants.boidsLayerName,
            Constants.predatorsLayerName
        );
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

    private void OnValidate()
    {
        Time.timeScale = timeScale;

        CheckBoundsForDynamicRangeParameters();
        PreCalculateParameters();
        AdjustPredatorsColliders();
    }

    /// <summary>Pre-calculate all the entities parameters that require it.</summary>
    private void PreCalculateParameters()
    {
        boidsParams.PreCalculateParameters(
            calculationInterval, smoothnessRadiusOffset, numberOfBoids
        );
        predatorsParams.PreCalculateParameters(
            calculationInterval, smoothnessRadiusOffset, numberOfBoids
        );
    }

    /// <summary>Adjusts values of specific parameters to meets requirements that link them.</summary>
    private void CheckBoundsForDynamicRangeParameters()
    {
        if (boidsParams.cohesionRadius < boidsParams.separationRadius)
            boidsParams.cohesionRadius = boidsParams.separationRadius;
    }

    /// <summary> Spawn/Despawn a number of entities of a specific type. </summary> 
    /// <param name="nbToSpawn">The number of entities to spawn (could be negative if you want to despawn entities).</param>
    /// <param name="type">The type of entities to spawn.</param>
    private void AdjustNbEntities(int nbToSpawn, EntityType type)
    {
        if (nbToSpawn == 0)
            return;

        if (nbToSpawn > 0)
            SpawnEntities(nbToSpawn, type);
        else
            DespawnEntities(-nbToSpawn, type);
    }

    /// <summary>Spawn a number of entities of a specific type.</summary> 
    /// <param name="nbToSpawn">The number of entities to spawn.</param>
    /// <param name="type">The type of entities to spawn.</param>
    private void SpawnEntities(int nbToSpawn, EntityType type)
    {
        GameObject entityPrefab = (type == EntityType.BOID) ?
            boidsParams.prefab : predatorsParams.prefab;
        List<GameObject> entitiesList = (type == EntityType.BOID) ?
            boids : predators;
        float verticalRestriction = (type == EntityType.BOID) ?
            boidsParams.rwVerticalRestriction : predatorsParams.rwVerticalRestriction;

        for (int _ = 0; _ < nbToSpawn; _++)
        {
            Vector3 entityDirection = MathHelpers.
                GetRandomDirection(verticalRestriction);
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

    /// <summary>Despawn a number of entities of a specific type.</summary> 
    /// <param name="nbToDespawn">The number of entities to despawn.</param>
    /// <param name="type">The type of entities to despawn.</param>
    private void DespawnEntities(int nbToDespawn, EntityType type)
    {
        int startIndex = currentNbBoids - 1;
        int endIndex = currentNbBoids - nbToDespawn;
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

        IncrCurrentNbEntities(-nbToDespawn, type);
    }

    /// <summary>
    /// Adds an increment to <c>currentNbBoids</c> or <c>currentNbPredators</c> depending on the type argument.
    /// </summary> 
    /// <param name="increment">The increment to add.</param>
    /// <param name="type">The type of entities whose related counter you want to increment.</param>
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

    /// <summary>Set a random scale for an entity according to the parameters related to its type.</summary> 
    /// <param name="entity">The entity whose scale need to be changed.</param>
    /// <param name="type">The type of the entity.</param>
    private void SetEntityScale(GameObject entity, EntityType type)
    {
        float scaleVariations = (type == EntityType.BOID) ?
            boidsParams.scaleVariations : predatorsParams.scaleVariations;

        entity.transform.localScale *= (1 + scaleVariations * Random.Range(-1, 1));
    }

    /// <summary>Finds a random position to spawn an entity in the area related to its entity type.</summary>
    /// <param name="type">The type of the entity.</param>
    /// <returns>A random position to spawn the entity.</returns>
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

    /// <summary>Adjusts the size of predators colliders components according to parameters.</summary>
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
}
