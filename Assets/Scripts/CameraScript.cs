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
    public float margin;
    [Range(0, 10)]
    public float topMargin;

    private AreaScript area;

    private Vector3 center;
    private float distance;
    private float theta;
    private float phi;

    private Vector3 oldCenter;
    private float oldDistance;
    private float oldTheta;
    private float oldPhi;

    private Vector3 dragOrigin;
    private Vector3 centerDragOrigin;

    private bool needUpdate = false;

    void Start()
    {
        area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        center = area.transform.position + initialOffset;

        (distance, theta, phi) = MathHelpers.CartesianToSpherical(
            center,
            transform.position
        );

        UpdateOldValues();
        UpdateRotation();
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

    private void UpdateRotation()
    {
        Vector3 cameraToCenter = center - transform.position;
        transform.rotation = Quaternion.LookRotation(cameraToCenter);
    }

    private void UpdatePosition()
    {
        float epsilon = 0.0001f;
        phi = Mathf.Clamp(phi, epsilon, Mathf.PI - epsilon);
        distance = Mathf.Clamp(distance, epsilon, maxDistance);

        if (!MathHelpers.IsInBox(
                center,
                area.minPt + margin * Vector3.one,
                area.maxPt - margin * Vector3.one
            )
        )
        {
            RestoreOldValues();
            return;
        }

        Vector3 newPosition = MathHelpers.SphericalToCartesian(
            center, distance, theta, phi);

        if (MathHelpers.IsInBox(
                newPosition,
                area.minPt - margin * Vector3.one,
                area.maxPt + new Vector3(margin, topMargin, margin)
            )
        )
        {
            RestoreOldValues();
            return;
        }

        transform.position = newPosition;

        UpdateOldValues();
        UpdateRotation();
    }

    private Vector3 Clamp3D(Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min.x, max.x),
            Mathf.Clamp(value.y, min.y, max.y),
            Mathf.Clamp(value.z, min.z, max.z)
        );
    }

    private void UpdateOldValues()
    {
        oldCenter = center;
        oldDistance = distance;
        oldTheta = theta;
        oldPhi = phi;
    }

    private void RestoreOldValues()
    {
        center = oldCenter;
        distance = oldDistance;
        theta = oldTheta;
        phi = oldPhi;
    }
}
