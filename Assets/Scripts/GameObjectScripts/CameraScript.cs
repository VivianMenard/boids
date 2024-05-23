using UnityEngine;

/// <summary>
/// Manages all camera movements.
/// </summary>
public class CameraScript : MonoBehaviour
{
    [SerializeField, Tooltip("The area where the camera cannot go.")]
    private GameObject cameraExclusionArea;
    [SerializeField, Tooltip("The offset of the initial center of the camera from the center of the exclusion area.")]
    private Vector3 initialOffset;
    [SerializeField, Range(0, 0.05f), Tooltip("In rad/screenUnit, rotation speed of the camerag.")]
    private float dragSpeed;
    [SerializeField, Range(0, 0.1f), Tooltip("In u/sreenUnit, center moving speed.")]
    private float centerDragSpeed;
    [SerializeField, Range(0, 30), Tooltip("In u/scroolUnit, Zoom speed when scrooling.")]
    private float scrollSpeed;
    [SerializeField, Range(0, 200), Tooltip("Maximum distance of the camera from the center.")]
    private float maxDistance;
    [SerializeField, Range(0, 5), Tooltip("Margin with respect to the camera exclusion area.")]
    private float margin;

    [SerializeField, Range(0, 1), Tooltip("In seconds, duration of the camera centering animation.")]
    private float centeringAnimationTotalTime;
    [SerializeField, Range(0, 2), Tooltip("In u, distance from origin under which the camera is considered non moved.")]
    private float thresholdForOriginCriteria;
    [SerializeField, Range(0, 0.1f), Tooltip("In rad, angular distance from origin under which the camera is considered non moved.")]
    private float angularThresholdForOriginCriteria;
    private float centeringTime;
    private bool centeringInProgress = false;
    private Vector3 initialCenter;
    private Vector3 beginingAnimCenter;
    private float initialDistance, initialTheta, initialPhi;
    private float beginingAnimDistance, beginingAnimTheta, beginingAnimPhi;

    private Vector3 areaMinPtForCamera, areaMaxPtForCamera,
    areaMinPtForCenter, areaMaxPtForCenter;

    private Vector3 center;
    private float distance, theta, phi;

    private Vector2 referenceForTheta;
    private float thetaOffset;

    private Vector3 dragOrigin, centerDragOrigin;

    private bool needUpdate = false;

    void Start()
    {
        AreaScript area = GetArea();

        areaMinPtForCamera = area.MinPt - margin * Vector3.one;
        areaMaxPtForCamera = area.MaxPt + margin * Vector3.one;
        areaMinPtForCenter = area.MinPt + margin * Vector3.one;
        areaMaxPtForCenter = area.MaxPt - margin * Vector3.one;

        center = area.transform.position + initialOffset;

        ComputeThetaReference();
        (distance, theta, phi) = MathHelpers.CartesianToSpherical(
            center,
            transform.position,
            referenceForTheta
        );

        initialCenter = center;
        initialDistance = distance;
        initialTheta = theta;
        initialPhi = phi;

        UpdateRotation();
    }

    void Update()
    {
        if (centeringInProgress)
        {
            ProcessCenteringAnimation();
            return;
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            StartCenteringAnimation();
            return;
        }

        float scrollValue = Input.GetAxis(Constants.scrollAxisName);

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
    }

    void FixedUpdate()
    {
        if (needUpdate)
        {
            UpdatePosition();
            needUpdate = false;
        }
    }

    /// <summary>
    /// Adjusts the camera position to put it at max distance from the center, and directs it 
    /// toward the center.
    /// </summary>
    [ContextMenu("Adjust camera transform")]
    private void AdjustCameraTransform()
    {
        AreaScript area = GetArea();

        center = area.transform.position + initialOffset;

        Vector3 cameraDirection = (transform.position - center).normalized;
        transform.position = center + cameraDirection * maxDistance;

        UpdateRotation();
    }

    /// <summary>
    /// Allows to get the AreaScript component in the exclusion area gameObject.
    /// </summary>
    /// 
    /// /// <returns>
    /// The AreaScript.
    /// </returns>
    private AreaScript GetArea()
    {
        AreaScript area = cameraExclusionArea.GetComponent<AreaScript>();
        if (area == null)
            throw new MissingComponentException(
                "No AreaScript component on cameraExclusionArea GameObject."
            );

        return area;
    }

    /// <summary>
    /// Compute <c>referenceForTheta</c> and <c>thetaOffset</c> in order to have a spherical coordinate system
    /// where initial camera position corresponds to <c>theta = 0</c>.
    /// </summary>
    private void ComputeThetaReference()
    {
        Vector3 centerToPosition = transform.position - center;
        Vector2 centerToPositionXZ = new Vector2(centerToPosition.x, centerToPosition.z);

        referenceForTheta = centerToPositionXZ.normalized;
        thetaOffset = Vector2.Angle(centerToPositionXZ, Vector2.right) * Mathf.Deg2Rad;
    }

    /// <summary>
    /// Determines if the camera is at its original position.
    /// </summary>
    /// 
    /// <returns>
    /// <c>true</c> if the camera is at its original position; otherwise, <c>false</c>.
    /// </returns>
    private bool IsAtOrigin()
    {
        bool isInitialCenter =
                Vector3.Distance(center, initialCenter) < thresholdForOriginCriteria,
            isInitialDistance =
                Mathf.Abs(distance - initialDistance) < thresholdForOriginCriteria,
            IsInitialTheta =
                Mathf.Abs(theta - initialTheta) < angularThresholdForOriginCriteria,
            IsInitialPhi =
                Mathf.Abs(phi - initialPhi) < angularThresholdForOriginCriteria;

        return isInitialCenter && isInitialDistance && IsInitialTheta && IsInitialPhi;
    }

    /// <summary>
    /// Starts the centering animation the camera is not at its original position.
    /// </summary>
    private void StartCenteringAnimation()
    {
        if (IsAtOrigin())
            return;

        centeringInProgress = true;

        centeringTime = 0f;

        beginingAnimCenter = center;
        beginingAnimDistance = distance;
        beginingAnimTheta = MathHelpers.EquivalentInTrigoRange(theta);
        beginingAnimPhi = phi;
    }

    /// <summary>
    /// Continues centering animation.
    /// </summary>
    private void ProcessCenteringAnimation()
    {
        centeringTime += Time.deltaTime;
        float progress = Mathf.SmoothStep(
            0,
            1,
            Mathf.Clamp(centeringTime / centeringAnimationTotalTime, 0, 1)
        );

        center = Vector3.Lerp(beginingAnimCenter, initialCenter, progress);
        distance = Mathf.Lerp(beginingAnimDistance, initialDistance, progress);
        theta = Mathf.Lerp(beginingAnimTheta, initialTheta, progress);
        phi = Mathf.Lerp(beginingAnimPhi, initialPhi, progress);

        if (centeringTime >= centeringAnimationTotalTime)
            EndCenteringAnimation();

        needUpdate = true;
    }

    /// <summary>
    /// Ends centering animation.
    /// </summary>
    private void EndCenteringAnimation()
    {
        centeringInProgress = false;

        dragOrigin = Input.mousePosition;
        centerDragOrigin = Input.mousePosition;
    }

    /// <summary>
    /// Updates the rotation of the camera to direct it to <c>center</c>.
    /// </summary>
    private void UpdateRotation()
    {
        Vector3 cameraToCenter = center - transform.position;
        transform.rotation = Quaternion.LookRotation(cameraToCenter);
    }

    /// <summary>
    /// Update camera position according to <c>center</c>, <c>distance</c>, <c>theta</c> and <c>phi</c>.
    /// </summary>
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
            center, distance, theta, phi, thetaOffset);

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
                center, newPosition, referenceForTheta);
        }

        transform.position = newPosition;
        UpdateRotation();
    }
}
