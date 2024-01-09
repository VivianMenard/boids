using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidsManagerScript : MonoBehaviour
{
    public float velocity;
    public int maxVisionDistance;
    public float idealNbNeighbors;
    public float visionAngle;

    public float momentumStrengh;
    public float alignmentStrengh;

    [SerializeField] private int numberOfBoids; 
    [SerializeField] private GameObject Boid;
    private AreaScript area;
    void Start() {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        SpawnBoids();
    }
    void Update() {}

    private void SpawnBoids() {
        for (int _ = 0; _ < numberOfBoids; _++) {
            Instantiate(
                Boid,  
                GetRandomPositionInArea(),
                Quaternion.identity
            );
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
