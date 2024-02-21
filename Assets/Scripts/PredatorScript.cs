using UnityEngine;

public class PredatorScript : EntityScript
{
    private PredatorsParameters predatorsParams;
    private float currentAnimationPhase;

    protected override void InitParams()
    {
        parameters = entitiesManager.predatorsParams;
        predatorsParams = (PredatorsParameters)parameters;
        currentAnimationPhase = Random.Range(0, 2 * Mathf.PI);
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

                float preyWeight = GetEntityWeightAccordingToVisionDistance(
                    squaredDistance);

                nbPreys += preyWeight;
                preysPositionsSum += preyWeight * entityPosition;
            }
            else if (IsPredatorCollider(entityCollider))
            {
                float peerWeight = GetEntityPeerRepulsionWeight(squaredDistance);

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

    private float GetEntityPeerRepulsionWeight(float squaredDistance)
    {
        return InverseLerpOpti(
            predatorsParams.squaredPeerRepulsionRadius,
            predatorsParams.squaredFullPeerRepulsionRadius,
            predatorsParams.peerRepulsionSmoothRangeSizeInverse,
            squaredDistance
        );
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

    protected override void ComputeBonesPositionsAndRotations()
    {
        base.ComputeBonesPositionsAndRotations();

        AddWaveMotion();
        smoothBonesRotations();
    }

    private void AddWaveMotion()
    {
        Vector3 right = Vector3.Cross(Direction, Vector3.up);

        float velocityFactor = 1 +
            predatorsParams.velocityImpactOnWaves *
            (velocity / parameters.velocities[parameters.defaultState] - 1);

        float speed = velocityFactor * predatorsParams.wavesBaseSpeed;
        float magnitude = velocityFactor * predatorsParams.wavesBaseMagnitude;

        currentAnimationPhase += speed % (2 * Mathf.PI);

        for (int boneIndex = parameters.animationFirstBone; boneIndex < bones.Length; boneIndex++)
        {
            (Vector3 position, Quaternion rotation) = bonesPositionsAndRotations[boneIndex];
            float distanceToHead = BoneDistanceToHead(boneIndex);
            Vector3 newPosition = position + magnitude * Enveloppe(distanceToHead) * Wave(
                predatorsParams.wavesBaseSpacialFrequency * distanceToHead - currentAnimationPhase) * right;
            bonesPositionsAndRotations[boneIndex] = (newPosition, rotation);
        }
    }

    private float Enveloppe(float x)
    {
        return predatorsParams.wavesEnveloppeGradient * x + predatorsParams.wavesEnveloppeMin;
    }

    private float Wave(float x)
    {
        return Mathf.Sin(x);
    }

    private void smoothBonesRotations()
    {
        for (int boneIndex = parameters.animationFirstBone; boneIndex < bones.Length - 1; boneIndex++)
        {
            (Vector3 position, Quaternion _) = bonesPositionsAndRotations[boneIndex];
            (Vector3 nextPosition, Quaternion _) = bonesPositionsAndRotations[boneIndex + 1];

            Quaternion newRotation = Quaternion.LookRotation(position - nextPosition);
            bonesPositionsAndRotations[boneIndex] = (position, newRotation);
        }
    }
}
