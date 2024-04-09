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
        return value >= min && value <= max;
    }

    public static bool IsInBox(Vector3 point, Vector3 minPt, Vector3 maxPt)
    {
        return (
            IsBetween(point.x, minPt.x, maxPt.x) &&
            IsBetween(point.y, minPt.y, maxPt.y) &&
            IsBetween(point.z, minPt.z, maxPt.z)
        );
    }
}