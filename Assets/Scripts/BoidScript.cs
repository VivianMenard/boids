using UnityEngine;

public class BoidScript: EntityScript
{
    private BoidsParameters boidsParams;

    protected override void InitParams() {
        parameters = entitiesManager.boidsParams;
        boidsParams = (BoidsParameters)parameters;
    }

    protected override void ComputeNewDirection() {
        Collider[] nearbyColliders = GetNearbyColliders();

        Vector3 separationPositionSum = Vector3.zero,
            alignmentDirectionSum = Vector3.zero,
            cohesionPositionSum = Vector3.zero,
            predatorsPositionSum = Vector3.zero;

        int nbBoidsSeparation = 0,
            nbBoidsAlignment = 0,
            nbBoidsCohesion = 0,
            nbPredators = 0;

        foreach (Collider collider in nearbyColliders) {
            if (IsMyCollider(collider))
                continue;

            if (IsBoidCollider(collider)) {
                if (!IsInMyFOV(collider))
                    continue;

                float squaredDistance = (
                    collider.transform.position - transform.position
                ).sqrMagnitude;

                if (squaredDistance > boidsParams.squaredCohesionRadius) {
                    nbBoidsCohesion += 1;
                    cohesionPositionSum += collider.transform.position;
                } 
                else if (squaredDistance < boidsParams.squaredSeparationRadius) {
                    nbBoidsSeparation += 1;
                    separationPositionSum += collider.transform.position;
                } 
                else {
                    nbBoidsAlignment += 1;
                    BoidScript boidScript = collider.GetComponent<BoidScript>();
                    alignmentDirectionSum += boidScript.Direction;
                }
            } 
            else if (IsPredatorCollider(collider)) {
                nbPredators++;
                predatorsPositionSum += collider.transform.position;
            }
        }

        AdaptVisionDistance(nbBoidsSeparation + nbBoidsAlignment + nbBoidsCohesion);
        velocityBonusActivated = nbPredators != 0;

        float separationWeight = GetBehaviorWeight(
                nbBoidsSeparation, boidsParams.separationWeight),
            alignmentWeight = GetBehaviorWeight(
                nbBoidsAlignment, boidsParams.alignmentWeight),
            cohesionWeight = GetBehaviorWeight(
                nbBoidsCohesion, boidsParams.cohesionWeight),
            fearWeight = GetBehaviorWeight(nbPredators, boidsParams.fearWeight);

        float weightSum = boidsParams.momentumWeight + separationWeight +
            alignmentWeight + cohesionWeight + fearWeight;

        if (weightSum == 0)
            return;

        Vector3 separationDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, separationPositionSum, nbBoidsSeparation),
            alignmentDirection = GetIdealDirectionForBehavior(
                Behavior.ALIGNMENT, alignmentDirectionSum, nbBoidsAlignment),
            cohesionDirection = GetIdealDirectionForBehavior(
                Behavior.COHESION, cohesionPositionSum, nbBoidsCohesion),
            fearDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, predatorsPositionSum, nbPredators);

        Vector3 newDirection = (
            (
                boidsParams.momentumWeight * Direction + 
                separationWeight * separationDirection +
                alignmentWeight * alignmentDirection + 
                cohesionWeight * cohesionDirection + 
                fearWeight * fearDirection
            ) / weightSum
        ).normalized;

        SetDirection(newDirection);
    }

    private void AdaptVisionDistance(int nbBoidsInFOV) {
        if (nbBoidsInFOV > boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Max(1, visionDistance - 1);
        else if (nbBoidsInFOV < boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Min(boidsParams.visionDistance, visionDistance + 1);
    }
}
