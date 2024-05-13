using UnityEngine;

/// <summary>
/// A collection of static helper methods for mathematical operations.
/// </summary>
public static class MathHelpers
{
    /// <summary> A constant representing 1 / √2.</summary>
    public const float OneOverSquareRootOfTwo = 0.7071067812f;

    /// <summary>Squares a number.</summary> 
    /// <param name="number">The number to square.</param> 
    /// <returns>The squared number</returns>
    public static float Square(float number)
    {
        return number * number;
    }

    /// <summary>
    /// Computes cartesian coordinates of a point from its spherical coordinates (mathematics convention).
    /// </summary>
    /// 
    /// <param name="center">The center of the spherical coordinates system.</param>
    /// <param name="distance">
    /// The radial distance in spherical coordinates (in R+) (distance between the point and the center).
    /// </param>
    /// <param name="theta">The azimuthal angle of the point in spherical coordinates (in [-π, π[).</param>
    /// <param name="phi">The polar angle of the point in spherical coordinates (in [0, π]).</param>
    /// <param name="thetaOffset">
    /// [optional] Default 0, offset for theta in case the reference vector for theta angle isn't 
    /// the x axis of the cartesian coordinate system.
    /// </param>
    /// 
    /// <returns>
    /// The cartesian coordinates of the point, in a coordinates system :
    ///     - Whose center is the same than the one of the spherical coordinate system;
    ///     - Whose x axis is the reference for the azimuthal angle (in thetaOffset = 0);
    ///     - Whose y axis is the reference for the polar angle.
    /// </returns>
    public static Vector3 SphericalToCartesian(
        Vector3 center, float distance, float theta, float phi, float thetaOffset = 0)
    {
        Vector3 direction = new Vector3(
           Mathf.Sin(phi) * Mathf.Cos(thetaOffset + theta),
           Mathf.Cos(phi),
           Mathf.Sin(phi) * Mathf.Sin(thetaOffset + theta)
       );

        return center + distance * direction;
    }

    /// <summary>
    /// Computes spherical coordinates of a point from its cartesian coordinates (mathematics convention).
    /// </summary>
    /// 
    /// <param name="center">The center of the cartesian coordinates system.</param>
    /// <param name="cartesianPosition">The position of the point in the cartesian coordiantes system.</param>
    /// <param name="referenceForTheta">[Optional] Default <c>Vector2(1, 0)</c>, the reference to compute theta from (in the XZ plan).</param>

    /// <returns>
    /// The spherical coordinates of the point as a tuple (
    ///     radial distance (in R+), 
    ///     azimuthal angle theta in [-π, π[, 
    ///     polar angle in [0, π],
    /// ).
    ///     
    /// The spherical coordinates system is such that:
    ///     - Its center is the same than the one of the cartesian coordinate system;
    ///     - Its reference for the azimutal angle is <c>referenceForTheta</c> parameter;
    ///     - Its reference for the polar angle is the y axis of the cartesian coordinate system.
    /// </returns>
    public static (float, float, float) CartesianToSpherical(
        Vector3 center, Vector3 cartesianPosition, Vector2 referenceForTheta = default)
    {
        if (referenceForTheta == default)
            referenceForTheta = Vector2.right;

        Vector3 centerToPosition = cartesianPosition - center;
        Vector2 centerToPositionXZ = new Vector2(centerToPosition.x, centerToPosition.z);

        Vector2 referenceForThetaOrtho = new Vector2(-referenceForTheta.y, referenceForTheta.x);

        float thetaSign = Mathf.Sign(Vector2.Dot(referenceForThetaOrtho, centerToPositionXZ));

        float distance = centerToPosition.magnitude;
        float theta = thetaSign *
            Vector2.Angle(centerToPositionXZ, referenceForTheta) *
            Mathf.Deg2Rad;
        float phi = Vector3.Angle(Vector3.up, centerToPosition) * Mathf.Deg2Rad;

        return (distance, theta, phi);
    }

    /// <summary>
    /// Determines whether a value is between two bounds (exclusively).
    /// </summary>
    /// 
    /// <param name="value">The value to check.</param>
    /// <param name="min">The minimum bound (exclusive).</param>
    /// <param name="max">The maximum bound (exclusive).</param>
    /// 
    /// <returns>
    /// <c>true</c> if the value is between the bounds (exclusively); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsBetween(float value, float min, float max)
    {
        return value > min && value < max;
    }

    /// <summary>
    /// Determines whether a point is inside a axis aligned box defined by its minimum and maximum points (exclusive).
    /// </summary>
    /// 
    /// <param name="point">The point to check.</param>
    /// <param name="minPt">The minimum point of the box (exclusive).</param>
    /// <param name="maxPt">The maximum point of the box (exclusive).</param>
    /// 
    /// <returns>
    /// <c>true</c> if the point is inside the box (exclusive); otherwise, <c>false</c>.
    /// </returns>
    public static bool IsInBox(Vector3 point, Vector3 minPt, Vector3 maxPt)
    {
        return (
            IsBetween(point.x, minPt.x, maxPt.x) &&
            IsBetween(point.y, minPt.y, maxPt.y) &&
            IsBetween(point.z, minPt.z, maxPt.z)
        );
    }

    /// <summary>
    /// Clamps each component of a vector within specified minimum and maximum values.
    /// </summary>
    /// 
    /// <param name="initialVector">The initial vector to clamp.</param>
    /// <param name="min">The minimum values for each component.</param>
    /// <param name="max">The maximum values for each component.</param>
    /// 
    /// <returns>
    /// A new vector with each component clamped within the specified range.
    /// </returns>
    public static Vector3 ClampVector(Vector3 initialVector, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(initialVector.x, min.x, max.x),
            Mathf.Clamp(initialVector.y, min.y, max.y),
            Mathf.Clamp(initialVector.z, min.z, max.z)
        );
    }

    /// <summary>
    /// Finds a point on the surface of an axis-aligned box, between two given points.
    /// </summary>
    /// 
    /// <param name="ptIn">The point inside the box.</param>
    /// <param name="ptOut">The point outside the box.</param>
    /// <param name="minPt">The minimum point of the box.</param>
    /// <param name="maxPt">The maximum point of the box.</param>
    /// 
    /// <returns>
    /// The point on the surface of the box between the two given points.
    /// </returns>
    public static Vector3 FindPointOnBoxBetween(Vector3 ptIn, Vector3 ptOut, Vector3 minPt, Vector3 maxPt)
    {
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

    /// <summary>
    /// Generates a random direction vector in 3D space, optionally restricting the vertical angle.
    /// </summary>
    /// 
    /// <param name="verticalRestriction">[Optional] Default 0, vertical restriction between 0 (no restrictions) and 1 (completely horizontal).</param>
    /// 
    /// <returns>
    /// A random direction vector in 3D space.
    /// </returns>
    public static Vector3 GetRandomDirection(float verticalRestriction = 0f)
    {
        float verticalRestrictionInRad = (Mathf.PI / 2) * Mathf.Clamp(
            verticalRestriction, 0, 1 - Mathf.Epsilon);

        float theta = Random.Range(0f, 2f * Mathf.PI);
        float phi = Random.Range(
            verticalRestrictionInRad,
            Mathf.PI - verticalRestrictionInRad
        );

        return SphericalToCartesian(
            Vector3.zero, 1f, theta, phi);
    }

    /// <summary>Converts a quaternion rotation to a forward direction vector.</summary> 
    /// <param name="rotation">The quaternion rotation to convert.</param> 
    /// <returns>The forward direction vector corresponding to the given rotation.</returns>
    public static Vector3 RotationToDirection(Quaternion rotation)
    {
        return rotation * Vector3.forward;
    }

    /// <summary>Converts an angle to its equivalent within the range [-π, π[.</summary> 
    /// <param name="initialAngle">The initial angle to convert in radians.</param>/// 
    /// <returns>The equivalent angle within the range [-π, π[.</returns>
    public static float EquivalentInTrigoRange(float initialAngle)
    {
        float moduloAngle = initialAngle % (2 * Mathf.PI);
        if (moduloAngle < 0)
            moduloAngle += (2 * Mathf.PI);

        if (moduloAngle <= Mathf.PI)
            return moduloAngle;

        return moduloAngle - 2 * Mathf.PI;
    }

    /// <summary>
    /// Restricts the vertical angle of a direction vector in 3D space.
    /// </summary>
    /// 
    /// <param name="initialDirection">The initial direction vector to restrict.</param>
    /// <param name="verticalRestriction">
    /// The vertical restriction to apply, between 0 (no restrictions) and 1 (completely horizontal).
    /// </param>
    /// 
    /// <returns>
    /// The direction vector with the vertical angle restricted.
    /// </returns>
    public static Vector3 RestrictDirectionVertically(Vector3 initialDirection, float verticalRestriction)
    {
        float verticalRestrictionInRad = (Mathf.PI / 2) * Mathf.Clamp(
            verticalRestriction, 0, 1 - Mathf.Epsilon);

        (float distance, float theta, float phi) = CartesianToSpherical(
            Vector3.zero, initialDirection);

        float restrictedPhi = Mathf.Clamp(
            phi, verticalRestrictionInRad, Mathf.PI - verticalRestrictionInRad);

        return SphericalToCartesian(
            Vector3.zero, distance, theta, restrictedPhi);
    }

    /// <summary>
    /// Generates two orthogonal axes around a given vector.
    /// </summary>
    /// 
    /// <param name="originalAxis">The main axis around which to generate the orthogonal axes.</param>
    /// <param name="reference">[Optional] Default <c>Vector3.up</c>, the reference vector to define one of the orthogonal axes.</param>
    /// 
    /// <returns>
    ///   A tuple containing two vectors representing the orthogonal axes generated around the main axis.
    ///   The first axis is perpendicular to the main axis and the reference vector.
    ///   The second axis is perpendicular to the main axis and the first axis.
    /// </returns>
    public static (Vector3, Vector3) GenerateOrthogonalAxesAroundVector(Vector3 originalAxis, Vector3 reference = default)
    {
        if (reference == default)
            reference = Vector3.up;

        Vector3 axis1 = Vector3.Cross(originalAxis, reference).normalized;
        Vector3 axis2 = Vector3.Cross(originalAxis, axis1).normalized;

        return (axis1, axis2);
    }

    /// <summary>
    /// Remaps a value from one range to another.
    /// </summary>
    /// 
    /// <param name="value">The value to remap.</param>
    /// <param name="fromMin">The minimum value of the original range.</param>
    /// <param name="fromMax">The maximum value of the original range.</param>
    /// <param name="toMin">The minimum value of the target range.</param>
    /// <param name="toMax">The maximum value of the target range.</param>
    /// 
    /// <returns>
    /// The value remapped from the original range to the target range.
    /// If the value exceeds the original range, it will be clamped to the range bounds.
    /// </returns>
    public static float Remap(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        float clampedValue = Mathf.Clamp(value, fromMin, fromMax);
        return toMin + (clampedValue - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }

    /// <summary>
    /// Generates a Bernoulli trial outcome based on a given probability of success.
    /// </summary>
    /// <param name="probaSuccess">The probability of success for the Bernoulli trial (between 0 and 1).</param>
    /// <returns>
    ///   <c>true</c> if the trial is successful (random value is less than the probability of success); otherwise, <c>false</c>.
    /// </returns>
    public static bool Bernoulli(float probaSuccess)
    {
        float randomValue = Random.Range(0f, 1f);
        return randomValue < probaSuccess;
    }
}