using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[System.Serializable]
public abstract class EntityParameters
{
    public GameObject prefab;

    [Space, Range(0, 20)]
    public float velocity;

    [Space, Range(0, 15)]
    public int visionDistance;
    [Range(0, 180)]
    public int visionSemiAngle;

    [Space, Range(0, 10)]
    public float momentumStrengh;
}

[System.Serializable]
public class BoidsParameters: EntityParameters
{
    [Range(0, 10)]
    public float separationStrengh;
    [Range(0, 10)]
    public float alignmentStrengh;
    [Range(0, 10)]
    public float cohesionStrengh;
    [Range(0, 10)]
    public float fearStrengh;
    
    [Space, Range(0, 15), Tooltip("Distance below which the boid will try to distance itself from others")]
    public float separationRadius;
    [Range(0, 15), Tooltip("Distance above which the boid will try to get closer to the others")]
    public float cohesionRadius;
    [HideInInspector] 
    public float squaredSeparationRadius=0;
    [HideInInspector] 
    public float squaredCohesionRadius=0;   

    [Space, Range(0, 15)]
    public int idealNbNeighbors;
}

[System.Serializable]
public class PredatorsParameters:EntityParameters
{
    [Range(0, 10)]
    public float preyAttractionStrengh;
}

public class EntitiesManagerScript : MonoBehaviour 
{
    [Range(1, 10), Space, Tooltip("Number of FixedUpdates between velocity calculations")]
    public int calculationInterval;

    [Range(0, 3000), SerializeField]
    private int numberOfBoids;
    private int currentNbBoids = 0;

    [Range(0, 5), SerializeField]
    private int numberOfPredators;
    private int currentNbPredators = 0;

    [HideInInspector] 
    public int clock = 0;

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

        boidsParams.squaredSeparationRadius = boidsParams.separationRadius * boidsParams.separationRadius;
        boidsParams.squaredCohesionRadius = boidsParams.cohesionRadius * boidsParams.cohesionRadius;
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
}
