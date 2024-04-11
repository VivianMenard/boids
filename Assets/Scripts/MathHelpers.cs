using UnityEngine;

public static class MathHelpers
{
    public const float OneOverSquareRootOfTwo = 0.7071067812f;

    public static float Square(float number)
    {
        return number * number;
    }

    public static Vector3 SphericalToCartesian(Vector3 center, float distance, float theta, float phi)
    {
        // mathematics convention 
        // distance in R+ (radial distance), theta in [-pi, pi[ (azimuthal angle), phi in [0, pi] (polar angle)

        Vector3 direction = new Vector3(
           Mathf.Sin(phi) * Mathf.Cos(theta),
           Mathf.Cos(phi),
           Mathf.Sin(phi) * Mathf.Sin(theta)
       );

        return center + distance * direction;
    }

    public static (float, float, float) CartesianToSpherical(Vector3 center, Vector3 cartesianPosition)
    {
        // mathematics convention 
        // distance in R+ (radial distance), theta in [-pi, pi[ (azimuthal angle), phi in [0, pi] (polar angle)

        Vector3 centerToPosition = cartesianPosition - center;
        Vector2 centerToPositionXZ = new Vector2(centerToPosition.x, centerToPosition.z);

        float distance = centerToPosition.magnitude;
        float theta = Mathf.Sign(centerToPosition.z) *
            Vector2.Angle(centerToPositionXZ, Vector2.right) * Mathf.Deg2Rad;
        float phi = Vector3.Angle(Vector3.up, centerToPosition) * Mathf.Deg2Rad;

        return (distance, theta, phi);
    }

    public static bool IsBetween(float value, float min, float max)
    {
        return value > min && value < max;
    }

    public static bool IsInBox(Vector3 point, Vector3 minPt, Vector3 maxPt)
    {
        return (
            IsBetween(point.x, minPt.x, maxPt.x) &&
            IsBetween(point.y, minPt.y, maxPt.y) &&
            IsBetween(point.z, minPt.z, maxPt.z)
        );
    }

    public static Vector3 ClampVector(Vector3 initialVector, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(initialVector.x, min.x, max.x),
            Mathf.Clamp(initialVector.y, min.y, max.y),
            Mathf.Clamp(initialVector.z, min.z, max.z)
        );
    }

    public static Vector3 FindPointOnBoxBetween(Vector3 ptIn, Vector3 ptOut, Vector3 minPt, Vector3 maxPt)
    {
        // Returns the point on the (axis aligned) box surface between a point in the box and another out of it. 
        float t = 0f;

        if (IsBetween(minPt.x, ptOut.x, ptIn.x))
            t = Mathf.InverseLerp(ptOut.x, ptIn.x, minPt.x);
        else if (IsBetween(maxPt.x, ptIn.x, ptOut.x))
            t = Mathf.InverseLerp(ptOut.x, ptIn.x, maxPt.x);
        else if (IsBetween(minPt.y, ptOut.y, ptIn.y))
            t = Mathf.InverseLerp(ptOut.y, ptIn.y, minPt.y);
        else if (IsBetween(maxPt.y, ptIn.y, ptOut.y))
            t = Mathf.InverseLerp(ptOut.y, ptIn.y, maxPt.y);
        else if (IsBetween(minPt.z, ptOut.z, ptIn.z))
            t = Mathf.InverseLerp(ptOut.z, ptIn.z, minPt.z);
        else if (IsBetween(maxPt.z, ptIn.z, ptOut.z))
            t = Mathf.InverseLerp(ptOut.z, ptIn.z, maxPt.z);

        return Vector3.Lerp(ptOut, ptIn, t);
    }

    public static Vector3 GetRandomDirection(bool restrictVerticaly = false)
    {
        float theta = Random.Range(0f, 2f * Mathf.PI);
        float phi;

        if (restrictVerticaly)
            phi = Random.Range(Mathf.PI / 4, 3 * Mathf.PI / 4);
        else
            phi = Random.Range(0f, Mathf.PI);

        return SphericalToCartesian(
            Vector3.zero, 1f, theta, phi);
    }

    public static Vector3 RotationToDirection(Quaternion rotation)
    {
        return rotation * Vector3.forward;
    }
}