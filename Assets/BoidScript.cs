using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidScript : MonoBehaviour
{
    public Vector3 Direction;
    void Start()
    {
        float maxSpeed = 10.0f;
        Direction = new Vector3(
            Random.Range(-maxSpeed, maxSpeed),
            Random.Range(-maxSpeed, maxSpeed),
            Random.Range(-maxSpeed, maxSpeed)
        );

        transform.rotation = Quaternion.LookRotation(Direction);
    }
    void Update()
    {
        transform.position = transform.position + Direction * Time.deltaTime;
    }
}
