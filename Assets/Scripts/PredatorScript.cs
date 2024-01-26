using UnityEngine;

public class PredatorScript : EntityScript
{
    private PredatorsParameters predatorsParams;

    protected override void InitParams()
    {
        parameters = entitiesManager.predatorsParams;
        predatorsParams = (PredatorsParameters)parameters;
        state = State.HUNTING;
    }

    protected override Vector3 ComputeNewDirection() {
        Collider[] nearbyEntityColliders = GetNearbyEntityColliders();

        Vector3 boidsPositionsSum = Vector3.zero;
        Vector3 predatorsPositionsSum = Vector3.zero;
        int nbBoidsInFOV = 0;
        int nbRelevantPredators = 0;

        foreach (Collider entityCollider in nearbyEntityColliders) {
            if (IsMyCollider(entityCollider))
                continue;

            if (IsBoidCollider(entityCollider)) {
                if (!IsInMyFOV(entityCollider))
                    continue;

                nbBoidsInFOV++;
                boidsPositionsSum += entityCollider.transform.position;
            } 
            else if (IsPredatorCollider(entityCollider)) {
                float squaredDistance = (
                    entityCollider.transform.position - transform.position
                ).sqrMagnitude;

                if (squaredDistance < predatorsParams.squaredPeerRepulsionRadius) {
                    nbRelevantPredators++;
                    predatorsPositionsSum += entityCollider.transform.position;
                }
            }
        }

        AdaptState(nbBoidsInFOV);

        float preyAttractionWeight = GetBehaviorWeight(nbBoidsInFOV, predatorsParams.preyAttractionWeight),
            peerRepulsionWeight = GetBehaviorWeight(nbRelevantPredators, predatorsParams.peerRepulsionWeight);

        float weightSum = predatorsParams.momentumWeight + preyAttractionWeight + peerRepulsionWeight;

        if (weightSum == 0)
            return Direction;

        Vector3 preyAttractionDirection = GetIdealDirectionForBehavior(
                Behavior.COHESION, boidsPositionsSum, nbBoidsInFOV),
            peerRepulsionDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, predatorsPositionsSum, nbRelevantPredators);

        Vector3 newDirection = (
            (
                predatorsParams.momentumWeight * Direction + 
                preyAttractionDirection * preyAttractionWeight +
                peerRepulsionDirection * peerRepulsionWeight
            ) / weightSum
        ).normalized;

        return newDirection;
    }

    private void AdaptState(int nbBoidsInFOV) {
        state = (nbBoidsInFOV > predatorsParams.nbPreyForBonusVelocity) ? State.ATTACKING: state = State.HUNTING;
    }
}
