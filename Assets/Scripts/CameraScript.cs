using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [Range(0, 0.05f)]
    public float dragSpeed;
    [Range(0, 30)]
    public float scrollSpeed;
    [Range(0, 200)]
    public float maxDistance;

    private AreaScript area;

    private float distance;
    private float theta;
    private float phi;

    private Vector3 dragOrigin;

    private bool needUpdate = false;

    void Start()
    {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        Vector3 areaToCamera = transform.position - area.transform.position;

        distance = areaToCamera.magnitude;
        theta = Mathf.Sign(transform.position.z) * Vector3.Angle(areaToCamera, Vector3.right) * Mathf.Deg2Rad;
        phi = Vector3.Angle(Vector3.up, areaToCamera) * Mathf.Deg2Rad;

        transform.rotation = Quaternion.LookRotation(-areaToCamera);
    }

    void Update()
    {
        float scrollValue = Input.GetAxis("Mouse ScrollWheel");

        if (scrollValue != 0)
        {
            distance -= scrollSpeed * scrollValue;
            needUpdate = true;
        }

        if (!Input.GetMouseButton(0))
            return;

        if (!Input.GetMouseButtonDown(0))
        {
            theta -= dragSpeed * (Input.mousePosition.x - dragOrigin.x);
            phi += dragSpeed * (Input.mousePosition.y - dragOrigin.y);
            needUpdate = true;
        }

        dragOrigin = Input.mousePosition;
    }

    void FixedUpdate()
    {
        if (needUpdate)
        {
            UpdatePosition();
            needUpdate = false;
        }
    }

    private void UpdatePosition()
    {
        float epsilon = 0.0001f;
        phi = Mathf.Clamp(phi, epsilon, Mathf.PI - epsilon);
        distance = Mathf.Clamp(distance, 0, maxDistance);

        Vector3 direction = new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Cos(phi),
            Mathf.Sin(phi) * Mathf.Sin(theta)
        );

        transform.position = area.transform.position + distance * direction;
        transform.rotation = Quaternion.LookRotation(-direction);
    }
}
