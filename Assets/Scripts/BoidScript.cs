using UnityEngine;

public class BoidScript: EntityScript
{
    private BoidsParameters boidsParams;

    protected override void InitParams() {
        parameters = entitiesManager.boidsParams;
        boidsParams = (BoidsParameters)parameters;
        state = State.NORMAL;
    }

    protected override Vector3 ComputeNewDirection() {
        Collider[] nearbyEntityColliders = GetNearbyEntityColliders();

        Vector3 separationPositionSum = Vector3.zero,
            alignmentDirectionSum = Vector3.zero,
            cohesionPositionSum = Vector3.zero,
            predatorsPositionSum = Vector3.zero;

        int nbBoidsSeparation = 0,
            nbBoidsAlignment = 0,
            nbBoidsCohesion = 0,
            nbPredators = 0;

        foreach (Collider entityCollider in nearbyEntityColliders) {
            if (IsMyCollider(entityCollider))
                continue;

            if (IsBoidCollider(entityCollider)) {
                if (!IsInMyFOV(entityCollider))
                    continue;

                float squaredDistance = (
                    entityCollider.transform.position - transform.position
                ).sqrMagnitude;

                if (squaredDistance > boidsParams.squaredCohesionRadius) {
                    nbBoidsCohesion += 1;
                    cohesionPositionSum += entityCollider.transform.position;
                } 
                else if (squaredDistance < boidsParams.squaredSeparationRadius) {
                    nbBoidsSeparation += 1;
                    separationPositionSum += entityCollider.transform.position;
                } 
                else {
                    nbBoidsAlignment += 1;
                    BoidScript boidScript = entityCollider.GetComponent<BoidScript>();
                    alignmentDirectionSum += boidScript.Direction;
                }
            } 
            else if (IsPredatorCollider(entityCollider)) {
                nbPredators++;
                predatorsPositionSum += entityCollider.transform.position;
            }
        }

        AdaptVisionDistance(nbBoidsSeparation + nbBoidsAlignment + nbBoidsCohesion);
        AdaptState(nearbyEntityColliders.Length - nbPredators, nbPredators);

        if (state == State.ALONE) 
            return RandomWalk();
        else
            rwState = RwState.NOT_IN_RW;

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
            return Direction;

        Vector3 separationDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, separationPositionSum, nbBoidsSeparation),
            alignmentDirection = GetIdealDirectionForBehavior(
                Behavior.ALIGNMENT, alignmentDirectionSum, nbBoidsAlignment),
            cohesionDirection = GetIdealDirectionForBehavior(
                Behavior.COHESION, cohesionPositionSum, nbBoidsCohesion),
            fearDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, predatorsPositionSum, nbPredators);

        return (
            boidsParams.momentumWeight * Direction + 
            separationWeight * separationDirection +
            alignmentWeight * alignmentDirection + 
            cohesionWeight * cohesionDirection + 
            fearWeight * fearDirection
        ).normalized;
    }

    private void AdaptState(int nbBoidsNearby, int nbPredatorsNearby) {
        if (nbPredatorsNearby > 0 ) 
            state = State.AFRAID;
        else if (nbBoidsNearby < boidsParams.nbBoidsNearbyToBeAlone)
            state = State.ALONE;
        else    
            state = State.NORMAL;
    }

    private void AdaptVisionDistance(int nbBoidsInFOV) {
        if (nbBoidsInFOV > boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Max(1, visionDistance - 1);
        else if (nbBoidsInFOV < boidsParams.idealNbNeighbors)
            visionDistance = Mathf.Min(boidsParams.visionDistance, visionDistance + 1);
    }
}
