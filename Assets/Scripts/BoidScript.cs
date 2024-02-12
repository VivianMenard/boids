using UnityEngine;

public class BoidScript : EntityScript
{
    private BoidsParameters boidsParams;

    protected override void InitParams()
    {
        parameters = entitiesManager.boidsParams;
        boidsParams = (BoidsParameters)parameters;
        state = State.NORMAL;
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
            if (IsMyCollider(entityCollider))
                continue;

            Vector3 entityPosition = entityCollider.transform.position;

            float squaredDistance = (entityPosition - transform.position)
                .sqrMagnitude;

            if (IsBoidCollider(entityCollider))
            {
                nbBoidsNearby++;

                if (!IsInMyFOV(entityCollider))
                    continue;

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

                BoidScript boidScript = entityCollider.GetComponent<BoidScript>();
                alignmentDirectionSum += boidAlignmentWeight * boidScript.Direction;
            }
            else if (IsPredatorCollider(entityCollider))
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

        if (weightSum == 0f)
            return Direction;

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
            boidsParams.momentumWeight * Direction +
            separationWeight * separationDirection +
            alignmentWeight * alignmentDirection +
            cohesionWeight * cohesionDirection +
            fearWeight * fearDirection
        ).normalized;
    }

    private float GetEntitySeparationWeight(float squaredDistance)
    {
        return InverseLerpOpti(
            boidsParams.squaredSeparationRadius,
            boidsParams.squaredFullSeparationRadius,
            boidsParams.separationSmoothRangeSizeInverse,
            squaredDistance
        );
    }

    private float GetEntityCohesionWeight(float squaredDistance)
    {
        return InverseLerpOpti(
            boidsParams.squaredCohesionRadius,
            boidsParams.squaredFullCohesionRadius,
            boidsParams.cohesionSmoothRangeSizeInverse,
            squaredDistance
        );
    }

    private float GetEntityFearWeight(float squaredDistance)
    {
        return InverseLerpOpti(
            boidsParams.squaredFearRadius,
            boidsParams.squaredFullFearRadius,
            boidsParams.fearSmoothRangeSizeInverse,
            squaredDistance
        );
    }

    private void AdaptState(int nbBoidsNearby, int nbPredatorsNearby)
    {
        if (nbPredatorsNearby > 0)
            state = State.AFRAID;
        else if (nbBoidsNearby < boidsParams.nbBoidsNearbyToBeAlone)
            state = State.ALONE;
        else
            state = State.NORMAL;
    }

    private void AdaptVisionDistance(int nbBoidsInFOV)
    {
        if (nbBoidsInFOV > boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Max(1, visionDistance - 1);
        else if (nbBoidsInFOV < boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Min(
                boidsParams.visionDistance, visionDistance + 1);
    }
}
