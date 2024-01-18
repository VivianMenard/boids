using UnityEngine;

public class PredatorScript : EntityScript
{
    private PredatorsParameters predatorsParams;

    protected override void InitParams()
    {
        parameters = entitiesManager.predatorsParams;
        predatorsParams = (PredatorsParameters)parameters;
    }

    protected override void ComputeNewDirection() {
        Collider[] nearbyColliders = GetNearbyColliders();

        Vector3 boidsPositionsSum = Vector3.zero;
        int nbBoidsInFOV = 0;

        foreach (Collider collider in nearbyColliders) {
            if (!IsAVisibleBoid(collider))
                continue;

            nbBoidsInFOV++;
            boidsPositionsSum += collider.transform.position;
        }

        float coeffSum = predatorsParams.momentumWeight + predatorsParams.preyAttractionWeight;

        if (nbBoidsInFOV == 0 || coeffSum == 0)
            return;

        velocityBonusActivated = nbBoidsInFOV > predatorsParams.nbPreyForBonusVelocity;

        Vector3 averagePosition = boidsPositionsSum / (float)nbBoidsInFOV;
        Vector3 attractionDirection = GetDirectionToPosition(averagePosition);

        Vector3 newDirection = (
            (
                predatorsParams.momentumWeight * Direction + 
                attractionDirection * predatorsParams.preyAttractionWeight
            ) / coeffSum
        ).normalized;

        SetDirection(newDirection);
    }

    private bool IsAVisibleBoid(Collider collider) {
        return !IsMyCollider(collider) && IsInMyFOV(collider) && IsBoidCollider(collider);
    }
}
