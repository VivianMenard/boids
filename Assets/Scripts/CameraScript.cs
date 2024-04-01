using UnityEngine;

public class CameraScript : MonoBehaviour
{
    public Vector3 initialOffset;
    [Range(0, 0.05f)]
    public float dragSpeed;
    [Range(0, 0.1f)]
    public float centerDragSpeed;
    [Range(0, 30)]
    public float scrollSpeed;
    [Range(0, 200)]
    public float maxDistance;
    [Range(0, 5)]
    public float centerMargin;

    private AreaScript area;

    private Vector3 center;
    private float distance;
    private float theta;
    private float phi;

    private Vector3 dragOrigin;
    private Vector3 centerDragOrigin;

    private bool needUpdate = false;

    void Start()
    {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        center = area.transform.position + initialOffset;

        Vector3 centerToCamera = transform.position - center;

        distance = centerToCamera.magnitude;
        theta = Mathf.Sign(transform.position.z) * Vector3.Angle(centerToCamera, Vector3.right) * Mathf.Deg2Rad;
        phi = Vector3.Angle(Vector3.up, centerToCamera) * Mathf.Deg2Rad;

        transform.rotation = Quaternion.LookRotation(-centerToCamera);
    }

    void Update()
    {
        float scrollValue = Input.GetAxis("Mouse ScrollWheel");

        if (scrollValue != 0)
        {
            distance -= scrollSpeed * scrollValue;
            needUpdate = true;
        }

        if (Input.GetMouseButton(0))
        {
            if (!Input.GetMouseButtonDown(0))
            {
                theta -= dragSpeed * (Input.mousePosition.x - dragOrigin.x);
                phi += dragSpeed * (Input.mousePosition.y - dragOrigin.y);
                needUpdate = true;
            }

            dragOrigin = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            if (!Input.GetMouseButtonDown(2))
            {
                Vector3 cameraToCenter = center - transform.position;

                Vector3 axis1 = Vector3.Cross(cameraToCenter, Vector3.up).normalized;
                Vector3 axis2 = Vector3.Cross(cameraToCenter, axis1).normalized;

                center += centerDragSpeed * (
                    (Input.mousePosition.x - centerDragOrigin.x) * axis1 +
                    (Input.mousePosition.y - centerDragOrigin.y) * axis2
                );

                needUpdate = true;
            }

            centerDragOrigin = Input.mousePosition;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            center = area.transform.position + initialOffset;
            needUpdate = true;
        }
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
        phi = Mathf.Clamp(phi, epsilon, Mathf.PI / 2);

        if (distance > maxDistance)
            distance = maxDistance;

        else if (distance < epsilon)
        {
            float offsetToApply = epsilon - distance;
            Vector3 cameraDirection = (center - transform.position).normalized;
            center += cameraDirection * offsetToApply;
            distance = epsilon;
        }

        center = Clamp3D(
            center,
            area.minPt + centerMargin * Vector3.one,
            area.maxPt - centerMargin * Vector3.one
        );

        Vector3 direction = new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Cos(phi),
            Mathf.Sin(phi) * Mathf.Sin(theta)
        );

        transform.position = center + distance * direction;
        transform.rotation = Quaternion.LookRotation(-direction);
    }

    private Vector3 Clamp3D(Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min.x, max.x),
            Mathf.Clamp(value.y, min.y, max.y),
            Mathf.Clamp(value.z, min.z, max.z)
        );
    }
}
