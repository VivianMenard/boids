using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaScript : MonoBehaviour
{
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public float minZ;
    public float maxZ;

    // Start is called before the first frame update
    void Start()
    {
        minX = transform.position.x - transform.localScale.x / 2;
        maxX = transform.position.x + transform.localScale.x / 2;
        minY = transform.position.y - transform.localScale.y / 2;
        maxY = transform.position.y + transform.localScale.y / 2;
        minZ = transform.position.z - transform.localScale.z / 2;
        maxZ = transform.position.z + transform.localScale.z / 2;        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
