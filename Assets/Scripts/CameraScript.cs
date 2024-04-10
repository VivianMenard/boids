using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField]
    private Vector3 initialOffset;
    [SerializeField, Range(0, 0.05f)]
    private float dragSpeed;
    [SerializeField, Range(0, 0.1f)]
    private float centerDragSpeed;
    [SerializeField, Range(0, 30)]
    private float scrollSpeed;
    [SerializeField, Range(0, 200)]
    private float maxDistance;
    [SerializeField, Range(0, 5)]
    private float margin;
    [SerializeField, Range(0, 10)]
    private float topMargin;

    private Vector3 initialCenter;

    private Vector3 areaMinPtForCamera, areaMaxPtForCamera,
    areaMinPtForCenter, areaMaxPtForCenter;

    private Vector3 center;
    private float distance, theta, phi;

    private Vector3 dragOrigin, centerDragOrigin;

    private bool needUpdate = false;

    void Start()
    {
        AreaScript area = GameObject.FindGameObjectWithTag("Area").
            GetComponent<AreaScript>();

        initialCenter = area.transform.position + initialOffset;
        center = initialCenter;

        areaMinPtForCamera = area.minPt - margin * Vector3.one;
        areaMaxPtForCamera = area.maxPt + new Vector3(margin, topMargin, margin);
        areaMinPtForCenter = area.minPt + margin * Vector3.one;
        areaMaxPtForCenter = area.maxPt - margin * Vector3.one;

        (distance, theta, phi) = MathHelpers.CartesianToSpherical(
            center,
            transform.position
        );

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
            center = initialCenter;
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

        center = MathHelpers.ClampVector(
            center,
            areaMinPtForCenter,
            areaMaxPtForCenter
        );

        Vector3 newPosition = MathHelpers.SphericalToCartesian(
            center, distance, theta, phi);

        if (MathHelpers.IsInBox(
                newPosition,
                areaMinPtForCamera,
                areaMaxPtForCamera
            )
        )
        {
            newPosition = MathHelpers.FindPointOnBoxBetween(
                newPosition,
                transform.position,
                areaMinPtForCamera,
                areaMaxPtForCamera
            );
            (distance, theta, phi) = MathHelpers.CartesianToSpherical(
                center, newPosition);
        }

        transform.position = newPosition;
        UpdateRotation();
    }
}
