using UnityEngine;

public class PredatorScript : EntityScript
{
    private PredatorsParameters predatorsParams;

    protected override void InitParams()
    {
        parameters = entitiesManager.predatorsParams;
        predatorsParams = (PredatorsParameters)parameters;
    }

    protected override Vector3 ComputeNewDirection() {
        Collider[] nearbyColliders = GetNearbyColliders();

        Vector3 boidsPositionsSum = Vector3.zero;
        Vector3 predatorsPositionsSum = Vector3.zero;
        int nbBoidsInFOV = 0;
        int nbRelevantPredators = 0;

        foreach (Collider collider in nearbyColliders) {
            if (IsMyCollider(collider))
                continue;

            if (IsBoidCollider(collider)) {
                if (!IsInMyFOV(collider))
                    continue;

                nbBoidsInFOV++;
                boidsPositionsSum += collider.transform.position;
            } 
            else if (IsPredatorCollider(collider)) {
                float squaredDistance = (
                    collider.transform.position - transform.position
                ).sqrMagnitude;

                if (squaredDistance < predatorsParams.squaredPeerRepulsionRadius) {
                    nbRelevantPredators++;
                    predatorsPositionsSum += collider.transform.position;
                }
            }
        }

        velocityBonusActivated = nbBoidsInFOV > predatorsParams.nbPreyForBonusVelocity;

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
}
