using UnityEngine;

public class BoidScript : EntityScript
{
    private BoidsParameters boidsParams;

    protected override void InitParams()
    {
        parameters = entitiesManager.boidsParams;
        boidsParams = (BoidsParameters)parameters;
    }

    protected override Vector3 ComputeNewDirection()
    {
        Collider[] nearbyEntityColliders = GetNearbyEntityColliders();

        Vector3 separationPositionSum = Vector3.zero,
            alignmentDirectionSum = Vector3.zero,
            cohesionPositionSum = Vector3.zero,
            predatorsPositionSum = Vector3.zero;

        int nbBoidsNearby = 0;

        float weightedNbBoidsSeparation = 0,
            weightedNbBoidsAlignment = 0,
            weightedNbBoidsCohesion = 0,
            weightedNbPredators = 0;

        foreach (Collider entityCollider in nearbyEntityColliders)
        {
            if (entityCollider == myCollider)
                continue;

            Vector3 entityPosition = entitiesManager.ColliderToPosition(entityCollider);
            float squaredDistance = (entityPosition - myPosition).sqrMagnitude;
            EntityType entityType = entitiesManager.ColliderToEntityType(entityCollider);

            if (entityType == EntityType.BOID)
            {
                nbBoidsNearby++;

                if (!IsInMyFOV(entityPosition))
                    continue;

                Vector3 boidDirection = entitiesManager.ColliderToDirection(entityCollider);

                float boidWeight = GetEntityWeightAccordingToVisionDistance(squaredDistance);

                float cohesionPortion = GetEntityCohesionWeight(squaredDistance),
                    separationPortion = GetEntitySeparationWeight(squaredDistance);
                float alignmentPortion = 1 - cohesionPortion - separationPortion;

                float boidSeparationWeight = boidWeight * separationPortion,
                    boidAlignmentWeight = boidWeight * alignmentPortion,
                    boidCohesionWeight = boidWeight * cohesionPortion;

                weightedNbBoidsSeparation += boidSeparationWeight;
                weightedNbBoidsAlignment += boidAlignmentWeight;
                weightedNbBoidsCohesion += boidCohesionWeight;

                separationPositionSum += boidSeparationWeight * entityPosition;
                cohesionPositionSum += boidCohesionWeight * entityPosition;
                alignmentDirectionSum += boidAlignmentWeight * boidDirection;
            }
            else if (entityType == EntityType.PREDATOR)
            {
                float predatorWeight = GetEntityFearWeight(squaredDistance);

                weightedNbPredators += predatorWeight;
                predatorsPositionSum += predatorWeight * entityPosition;
            }
        }

        AdaptVisionDistance(
            (int)(
                weightedNbBoidsSeparation +
                weightedNbBoidsAlignment +
                weightedNbBoidsCohesion
            )
        );
        AdaptState(nbBoidsNearby, (int)Mathf.Ceil(weightedNbPredators));

        if (state == State.ALONE)
            return RandomWalk();
        else
            rwState = RwState.NOT_IN_RW;

        float weightSum = weightedNbBoidsSeparation +
            weightedNbBoidsAlignment +
            weightedNbBoidsCohesion +
            weightedNbPredators;

        if (weightSum < Mathf.Epsilon)
            return direction;

        Vector3 separationDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, separationPositionSum, weightedNbBoidsSeparation),
            alignmentDirection = GetIdealDirectionForBehavior(
                Behavior.ALIGNMENT, alignmentDirectionSum, weightedNbBoidsAlignment),
            cohesionDirection = GetIdealDirectionForBehavior(
                Behavior.COHESION, cohesionPositionSum, weightedNbBoidsCohesion),
            fearDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, predatorsPositionSum, weightedNbPredators);

        float separationWeight = GetReelWeight(
                weightedNbBoidsSeparation, boidsParams.separationBaseWeight),
            alignmentWeight = GetReelWeight(
                weightedNbBoidsAlignment, boidsParams.alignmentBaseWeight),
            cohesionWeight = GetReelWeight(
                weightedNbBoidsCohesion, boidsParams.cohesionBaseWeight),
            fearWeight = GetReelWeight(
                weightedNbPredators, boidsParams.fearBaseWeight);

        return (
            boidsParams.momentumWeight * direction +
            separationWeight * separationDirection +
            alignmentWeight * alignmentDirection +
            cohesionWeight * cohesionDirection +
            fearWeight * fearDirection
        ).normalized;
    }

    protected override Vector3 GetObstacleAvoidanceReference()
    {
        return MathHelpers.GetRandomDirection();
    }

    /// <summary>
    /// Calculates the weight of another boid for <c>SEPARATION</c> behavior calculation,
    /// based on the squared distance between the two boids.
    /// </summary>
    /// <param name="squaredDistance">The squared distance between the two boids.</param>
    /// <returns>
    /// The weight of the other boid for <c>SEPARATION</c> behavior calculation.
    /// </returns>
    private float GetEntitySeparationWeight(float squaredDistance)
    {
        return Mathf.InverseLerp(
            boidsParams.squaredSeparationRadius,
            boidsParams.squaredFullSeparationRadius,
            squaredDistance
        );
    }

    /// <summary>
    /// Calculates the weight of another boid for <c>COHESION</c> behavior calculation,
    /// based on the squared distance between the two boids.
    /// </summary>
    /// <param name="squaredDistance">The squared distance between the two boids.</param>
    /// <returns>
    /// The weight of the other boid for <c>COHESION</c> behavior calculation.
    /// </returns>
    private float GetEntityCohesionWeight(float squaredDistance)
    {
        return Mathf.InverseLerp(
            boidsParams.squaredCohesionRadius,
            boidsParams.squaredFullCohesionRadius,
            squaredDistance
        );
    }

    /// <summary>
    /// Calculates the weight of a predator for fear behavior calculation,
    /// based on the squared distance between the boid and the predator.
    /// </summary>
    /// <param name="squaredDistance">The squared distance between the boid and the predator.</param>
    /// <returns>
    /// The weight of the predator for fear behavior calculation.
    /// </returns>
    private float GetEntityFearWeight(float squaredDistance)
    {
        return Mathf.InverseLerp(
            boidsParams.squaredFearRadius,
            boidsParams.squaredFullFearRadius,
            squaredDistance
        );
    }

    /// <summary>
    /// Adapts the state of the boid regarding the number of other nearby boids and the number of nearby predator.
    /// </summary>
    /// <param name="nbBoidsNearby">The number of nearby boids.</param>
    /// <param name="nbPredatorsNearby">The number of nearby predators.</param>
    private void AdaptState(int nbBoidsNearby, int nbPredatorsNearby)
    {
        if (nbPredatorsNearby > 0)
            state = State.AFRAID;
        else if (nbBoidsNearby < boidsParams.nbBoidsNearbyToBeAlone)
            state = State.ALONE;
        else
            state = State.NORMAL;
    }

    /// <summary>
    /// Adapts the vision distance of the boid regarding the number of other boids in its FOV.
    /// The more boids there are in its FOV, the shorter will be its vision distance, in order to
    /// have always approximatly <c>idealNbNeighbors</c> boids in its FOV.
    /// </summary>
    /// <param name="nbBoidsInFOV">The number of other boids in the FOV of the boid.</param>
    private void AdaptVisionDistance(int nbBoidsInFOV)
    {
        if (nbBoidsInFOV > boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Max(1, visionDistance - 1);
        else if (nbBoidsInFOV < boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Min(
                boidsParams.visionDistance, visionDistance + 1);
    }
}
