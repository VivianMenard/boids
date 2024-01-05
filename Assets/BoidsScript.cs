using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidsScript : MonoBehaviour
{
    public GameObject Boid;
    public int NumberOfBoids;
    private AreaScript area;
    void Start()
    {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        for (int i=0; i < NumberOfBoids; i++)
        {
            Vector3 randomPosition = new Vector3(
                UnityEngine.Random.Range(area.minX, area.maxX),
                UnityEngine.Random.Range(area.minY, area.maxY),
                UnityEngine.Random.Range(area.minZ, area.maxZ)
            );

            Instantiate(
                Boid,  
                randomPosition,
                Quaternion.identity
            );
        }
    }
    void Update()
    {
        
    }
}
