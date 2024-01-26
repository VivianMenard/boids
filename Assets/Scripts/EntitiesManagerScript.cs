using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class EntityParameters
{
    public GameObject prefab;

    [Range(0, 50), Tooltip("In u/s²")]
    public float acceleration;
    [Range(0, 50), Tooltip("In u/s²")]
    public float emergencyAcceleration;

    [Space, Range(0, 15)]
    public int visionDistance;
    [Range(0, 180)]
    public int visionSemiAngle;
    [HideInInspector]
    public float cosVisionSemiAngle;

    [Space, Range(0, 10)]
    public float momentumWeight;

    [HideInInspector]
    public Dictionary<State, float> velocities;
}

[System.Serializable]
public class BoidsParameters: EntityParameters
{
    [Range(0, 10)]
    public float separationWeight;
    [Range(0, 10)]
    public float alignmentWeight;
    [Range(0, 10)]
    public float cohesionWeight;
    [Range(0, 10)]
    public float fearWeight;
    
    [Space, Range(0, 15), Tooltip("Distance below which the boid will try to distance itself from others")]
    public float separationRadius;
    [Range(0, 15), Tooltip("Distance above which the boid will try to get closer to the others")]
    public float cohesionRadius;
    [HideInInspector] 
    public float squaredSeparationRadius;
    [HideInInspector] 
    public float squaredCohesionRadius;   

    [Space, Range(0, 15)]
    public int idealNbNeighbors;

    [Space, Range(0, 50), Tooltip("In u/s")]
    public float normalVelocity;
    [Range(0, 50), Tooltip("In u/s")]
    public float afraidVelocity;
}

[System.Serializable]
public class PredatorsParameters:EntityParameters
{
    [Range(0, 10), Tooltip("How attracted the predator is to boids")]
    public float preyAttractionWeight;
    [Range(0, 10), Tooltip("How repulsed the predator is to the other ones")]
    public float peerRepulsionWeight;

    [Space, Range(0, 15), Tooltip("Distance under which the predator will try to distance itself from others")]
    public float peerRepulsionRadius;
    [HideInInspector] 
    public float squaredPeerRepulsionRadius;
    [Range(0, 15)]
    public float preyRepulsionRadius;

    [Space, Range(0, 500), Tooltip("Number of prey in predator FOV above which it accelerates")]
    public int nbPreyForBonusVelocity;

    [Space, Range(0, 50), Tooltip("In u/s")]
    public float huntingVelocity;
    [Range(0, 50), Tooltip("In u/s")]
    public float attackingVelocity;
}

public class EntitiesManagerScript : MonoBehaviour 
{
    [Range(0, 3)]
    public float TimeScale;

    [Range(1, 10), Space, Tooltip("Number of FixedUpdates between velocity calculations")]
    public int calculationInterval;

    [HideInInspector] 
    public int clock = 0;

    [Space, Range(0, 3000), SerializeField]
    private int numberOfBoids;
    private int currentNbBoids = 0;

    [Range(0, 5), SerializeField]
    private int numberOfPredators;
    private int currentNbPredators = 0;

    [Space]
    public bool ObstaclesAvoidance;
    [Range(0, 15)]
    public float raycastDistance;
    [Range(0, 5)]
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

    private enum EntityType {
        BOID,
        PREDATOR
    }

    void Start() {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();
        obstacleLayerMask = LayerMask.GetMask("Obstacles");
        entitiesLayerMask = LayerMask.GetMask("Boids", "Predators");
    }

    private void PreCalculateParameters() {
        boidsParams.squaredSeparationRadius = Square(boidsParams.separationRadius);
        boidsParams.squaredCohesionRadius = Square(boidsParams.cohesionRadius);
        predatorsParams.squaredPeerRepulsionRadius = Square(predatorsParams.peerRepulsionRadius);

        boidsParams.cosVisionSemiAngle = Mathf.Cos(boidsParams.visionSemiAngle * Mathf.Deg2Rad);
        predatorsParams.cosVisionSemiAngle = Mathf.Cos(predatorsParams.visionSemiAngle * Mathf.Deg2Rad);

        boidsParams.velocities = new Dictionary<State, float>{
            {State.NORMAL, boidsParams.normalVelocity},
            {State.AFRAID, boidsParams.afraidVelocity},
        };

        predatorsParams.velocities = new Dictionary<State, float>{
            {State.HUNTING, predatorsParams.huntingVelocity},
            {State.ATTACKING, predatorsParams.attackingVelocity}
        };
    }

    private float Square(float value) {
        return value * value;
    }

    private float ComputeStep(float travelDistance, float totalDuration) {
        float stepDuration = Time.fixedDeltaTime;
        return travelDistance * stepDuration / totalDuration;
    }

    private void FixedUpdate() {
        clock = (clock + 1) % calculationInterval;

        AdjustNbEntities(numberOfBoids - currentNbBoids, EntityType.BOID);
        AdjustNbEntities(numberOfPredators - currentNbPredators, EntityType.PREDATOR);
    }

    private void AdjustNbEntities(int nbToSpawn, EntityType type) {
        if (nbToSpawn == 0) 
            return;
        
        if (nbToSpawn > 0)
            SpawnEntities(nbToSpawn, type);
        else
            DespawnEntities(-nbToSpawn, type);
    }

    private void SpawnEntities(int nbToSpawn, EntityType type) {
        GameObject entityPrefab = (type == EntityType.BOID) ? 
            boidsParams.prefab: predatorsParams.prefab;
        List<GameObject> entitiesList = (type == EntityType.BOID) ?
            boids: predators;

        for (int _ = 0; _ < nbToSpawn; _++) {
            GameObject entity = Instantiate(
                entityPrefab,  
                GetRandomPositionInArea(),
                Quaternion.identity
            );
            entity.transform.SetParent(gameObject.transform);
            entitiesList.Add(entity);
        }

        if (type == EntityType.BOID)
            currentNbBoids += nbToSpawn;
        else   
            currentNbPredators += nbToSpawn;
    }

    private void DespawnEntities(int nbToDespanw, EntityType type) {
        int startIndex = currentNbBoids - 1;
        int endIndex = numberOfBoids;
        List<GameObject> entitiesList = boids;

        if (type == EntityType.PREDATOR) {
            startIndex = currentNbPredators - 1;
            endIndex = numberOfPredators;
            entitiesList = predators;
        }

        for(int index = startIndex; index >= endIndex; index--){
            GameObject entity = entitiesList[index];
            entitiesList.RemoveAt(index);
            Destroy(entity);
        }

        if (type == EntityType.BOID)
            currentNbBoids -= nbToDespanw;
        else   
            currentNbPredators -= nbToDespanw;
    }

    private Vector3 GetRandomPositionInArea() {
        return new Vector3(
            UnityEngine.Random.Range(area.minPt.x, area.maxPt.x),
            UnityEngine.Random.Range(area.minPt.y, area.maxPt.y),
            UnityEngine.Random.Range(area.minPt.z, area.maxPt.z)
        );
    }

    private void OnValidate() {
        Time.timeScale = TimeScale;
        PreCalculateParameters();
        AdjustPredatorsColliders();
    }    

    private void AdjustPredatorsColliders() {
        AdjustPredatorCollider(predatorsParams.prefab);

        foreach(GameObject predator in predators)
            AdjustPredatorCollider(predator);
    }
         
    private void AdjustPredatorCollider(GameObject predator) {
        SphereCollider sphereCollider = predator.GetComponent<SphereCollider>();
        sphereCollider.radius = predatorsParams.preyRepulsionRadius;
    }
}