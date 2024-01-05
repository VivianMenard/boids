using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidsScript : MonoBehaviour
{
    public GameObject Boid;
    public int NumberOfBoids;
    public GameObject Area;
    void Start()
    {
        for (int i=0; i < NumberOfBoids; i++)
        {
            float maxOffsetX = Area.transform.localScale.x / 2;
            float maxOffsetY = Area.transform.localScale.y / 2;
            float maxOffsetZ = Area.transform.localScale.z / 2;

            Vector3 randomLocalPosition = new Vector3(
                UnityEngine.Random.Range(-maxOffsetX, maxOffsetX),
                UnityEngine.Random.Range(-maxOffsetY, maxOffsetY),
                UnityEngine.Random.Range(-maxOffsetZ, maxOffsetZ)
            );

            Vector3 randomPosition = Area.transform.position + randomLocalPosition;

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
