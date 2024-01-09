using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BoidsManagerScript : MonoBehaviour
{
    public int nbFrameBetweenUpdates;
    [HideInInspector] public int clock;
    public float velocity;
    public int maxVisionDistance;
    public float idealNbNeighbors;
    public float visionAngle;

    public float momentumStrengh;
    public float alignmentStrengh;
    public float cohesionStrengh;

    [SerializeField] private int numberOfBoids; 
    [SerializeField] private GameObject Boid;
    private AreaScript area;
    void Start() {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        clock = 0;

        SpawnBoids();
    }
    void Update() {}

    private void FixedUpdate() {
        clock = (clock + 1) % nbFrameBetweenUpdates;
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
