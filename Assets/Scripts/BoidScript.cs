using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidScript : MonoBehaviour
{
    public Vector3 Velocity;
    private AreaScript area;
    private BoidsManagerScript boidsManager;
    void Start() {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();
        boidsManager = GameObject.FindGameObjectWithTag("BoidsManager").
            GetComponent<BoidsManagerScript>();

        Velocity = GetRandomVelocity(
            boidsManager.minVelocity,
            boidsManager.maxVelocity
        );

        transform.rotation = Quaternion.LookRotation(Velocity);
    }
    void Update() {}

    private void FixedUpdate() {
        MoveForward();
        TeleportIfOutOfBorders(); 
    }

    private Vector3 GetRandomVelocity(float minVelocity, float maxVelocity) {
        return Random.Range(minVelocity, maxVelocity) * GetRandomDirection();
    }

    private Vector3 GetRandomDirection() {
        return new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        ).normalized;
    }

    private void MoveForward() {
        transform.position = transform.position + Velocity * Time.deltaTime;
    }

    private void TeleportIfOutOfBorders() {
        Vector3 newPosition = transform.position;

        if (transform.position.x < area.minPt.x) {
            newPosition.x = area.maxPt.x;
        } else if (transform.position.x > area.maxPt.x) {
            newPosition.x = area.minPt.x;
        }

        if (transform.position.y < area.minPt.y) {
            newPosition.y = area.maxPt.y;
        } else if (transform.position.y > area.maxPt.y) {
            newPosition.y = area.minPt.y;
        }

         if (transform.position.z < area.minPt.z) {
            newPosition.z = area.maxPt.z;
        } else if (transform.position.z > area.maxPt.z) {
            newPosition.z = area.minPt.z;
        }

        transform.position = newPosition;
    }
}
