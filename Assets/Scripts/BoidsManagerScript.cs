using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BoidsManagerScript : MonoBehaviour
{
    [Range(1, 10)]
    public int calculationInterval; // number of FixedUpdates between velocity calculations

    [Range(0, 20)]
    public float velocity;
    [Range(0, 15)]
    public int idealNbNeighbors;

    [Range(0, 15)]
    public float separationRadius;
    [Range(0, 15)]
    public float cohesionRadius;

    [Range(0, 15)]
    public int maxVisionDistance;
    [Range(0, 180)]
    public int visionSemiAngle;
    
    [Range(0, 10)]
    public float momentumStrengh;
    [Range(0, 10)]
    public float separationStrengh;
    [Range(0, 10)]
    public float alignmentStrengh;
    [Range(0, 10)]
    public float cohesionStrengh;

    [Range(0, 3000), SerializeField]
    private int numberOfBoids; 

    [HideInInspector] 
    public int clock = 0;

    [HideInInspector] 
    public float squaredSeparationRadius;
    [HideInInspector] 
    public float squaredCohesionRadius;

    [SerializeField] 
    private GameObject Boid;
    private AreaScript area;

    void Start() {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        squaredSeparationRadius = separationRadius * separationRadius;
        squaredCohesionRadius = cohesionRadius * cohesionRadius;

        SpawnBoids();
    }
    
    void Update() {}

    private void FixedUpdate() {
        clock = (clock + 1) % calculationInterval;
    }

    private void SpawnBoids() {
        for (int boidId = 0; boidId < numberOfBoids; boidId++) {
            GameObject boid = Instantiate(
                Boid,  
                GetRandomPositionInArea(),
                Quaternion.identity
            );
            BoidScript boidScript = boid.GetComponent<BoidScript>();
            boidScript.id = boidId;
        }
    }

    private Vector3 GetRandomPositionInArea() {
        return new Vector3(
            UnityEngine.Random.Range(area.minPt.x, area.maxPt.x),
            UnityEngine.Random.Range(area.minPt.y, area.maxPt.y),
            UnityEngine.Random.Range(area.minPt.z, area.maxPt.z)
        );
    }
}
