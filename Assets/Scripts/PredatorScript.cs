using UnityEngine;

public class PredatorScript : EntityScript
{
    private PredatorsParameters predatorsParams;

    protected override void InitParams()
    {
        parameters = entitiesManager.predatorsParams;
        predatorsParams = (PredatorsParameters)parameters;
        state = State.CHILLING;
    }

    protected override Vector3 ComputeNewDirection()
    {
        if (state == State.CHILLING)
        {
            AdaptState();
            return RandomWalk();
        }

        Collider[] nearbyEntityColliders = GetNearbyEntityColliders();

        Vector3 preysPositionsSum = Vector3.zero,
            peersPositionsSum = Vector3.zero;

        float nbPreys = 0f, nbPeers = 0f;

        foreach (Collider entityCollider in nearbyEntityColliders)
        {
            if (IsMyCollider(entityCollider))
                continue;

            Vector3 entityPosition = entityCollider.transform.position;
            float squaredDistance = (entityPosition - transform.position)
                .sqrMagnitude;

            if (IsBoidCollider(entityCollider))
            {
                if (!IsInMyFOV(entityCollider))
                    continue;

                float preyWeight = Mathf.InverseLerp(
                    visionDistance * visionDistance,
                    (visionDistance - 1) * (visionDistance - 1),
                    squaredDistance
                );

                nbPreys += preyWeight;
                preysPositionsSum += preyWeight * entityPosition;
            }
            else if (IsPredatorCollider(entityCollider))
            {
                float peerWeight = Mathf.InverseLerp(
                    predatorsParams.squaredPeerRepulsionRadius,
                    predatorsParams.squaredFullPeerRepulsionRadius,
                    squaredDistance
                );

                nbPeers += peerWeight;
                peersPositionsSum += peerWeight * entityPosition;
            }
        }

        AdaptState((int)nbPreys);

        if (nbPreys + nbPeers == 0f)
            return RandomWalk();
        else
            rwState = RwState.NOT_IN_RW;

        Vector3 preyAttractionDirection = GetIdealDirectionForBehavior(
                Behavior.COHESION, preysPositionsSum, nbPreys),
            peerRepulsionDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, peersPositionsSum, nbPeers);

        float preyAttractionWeight = GetReelWeight(
                nbPreys, predatorsParams.preyAttractionBaseWeight),
            peerRepulsionWeight = GetReelWeight(
                nbPeers, predatorsParams.peerRepulsionBaseWeight);

        return (
            predatorsParams.momentumWeight * Direction +
            preyAttractionWeight * preyAttractionDirection +
            peerRepulsionWeight * peerRepulsionDirection
        ).normalized;
    }

    private void AdaptState(int nbPreysInFOV = 0)
    {
        switch (state)
        {
            case State.CHILLING:
                if (Bernoulli(predatorsParams.probaHuntingAfterChilling))
                    state = State.HUNTING;
                break;

            case State.HUNTING:
                if (nbPreysInFOV > predatorsParams.nbPreysToAttack)
                    state = State.ATTACKING;
                else if (Bernoulli(predatorsParams.probaChillingAfterHunting))
                    state = State.CHILLING;
                break;

            case State.ATTACKING:
                if (nbPreysInFOV < predatorsParams.nbPreysToAttack)
                {
                    if (Bernoulli(predatorsParams.probaHuntingAfterAttacking))
                        state = State.HUNTING;
                    else
                        state = State.CHILLING;
                }
                break;
        }
    }
}
