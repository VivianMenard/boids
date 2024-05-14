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
        Collider[] nearbyEntityColliders = GetNearbyEntityColliders();

        Vector3 preysPositionsSum = Vector3.zero,
            peersPositionsSum = Vector3.zero;

        float weightedNbPreys = 0f, weightedNbPeers = 0f;

        foreach (Collider entityCollider in nearbyEntityColliders)
        {
            if (entityCollider == myCollider)
                continue;

            Vector3 entityPosition = entitiesManager.ColliderToPosition(entityCollider);
            float squaredDistance = (entityPosition - myPosition).sqrMagnitude;
            EntityType entityType = entitiesManager.ColliderToEntityType(entityCollider);

            if (entityType == EntityType.BOID)
            {
                if (state == State.CHILLING || !IsInMyFOV(entityPosition))
                    continue;

                float preyWeight = GetEntityWeightAccordingToVisionDistance(
                    squaredDistance);

                weightedNbPreys += preyWeight;
                preysPositionsSum += preyWeight * entityPosition;
            }
            else if (entityType == EntityType.PREDATOR)
            {
                float peerWeight = GetEntityPeerRepulsionWeight(squaredDistance);

                weightedNbPeers += peerWeight;
                peersPositionsSum += peerWeight * entityPosition;
            }
        }

        AdaptState((int)weightedNbPreys);

        if (weightedNbPreys + weightedNbPeers < Mathf.Epsilon)
            return RandomWalk();
        else
            rwState = RwState.NOT_IN_RW;

        Vector3 preyAttractionDirection = GetIdealDirectionForBehavior(
                Behavior.COHESION, preysPositionsSum, weightedNbPreys),
            peerRepulsionDirection = GetIdealDirectionForBehavior(
                Behavior.SEPARATION, peersPositionsSum, weightedNbPeers);

        float preyAttractionWeight = GetReelWeight(
                weightedNbPreys, predatorsParams.preyAttractionBaseWeight),
            peerRepulsionWeight = GetReelWeight(
                weightedNbPeers, predatorsParams.peerRepulsionBaseWeight);

        Vector3 idealNewDirection = (
            predatorsParams.momentumWeight * direction +
            preyAttractionWeight * preyAttractionDirection +
            peerRepulsionWeight * peerRepulsionDirection
        );

        return MathHelpers.RestrictDirectionVertically(
            idealNewDirection,
            predatorsParams.verticalRestriction
        );
    }

    protected override Vector3 GetObstacleAvoidanceReference()
    {
        return Vector3.up;
    }

    /// <summary>
    /// Calculates the weight of a predator for peer repulsion behavior calculation,
    /// based on the squared distance between the two predators.
    /// </summary>
    /// <param name="squaredDistance">The squared distance between the two predators.</param>
    /// <returns>
    /// The weight of the predator for peer repulsion behavior calculation.
    /// </returns>
    private float GetEntityPeerRepulsionWeight(float squaredDistance)
    {
        return Mathf.InverseLerp(
            predatorsParams.squaredPeerRepulsionRadius,
            predatorsParams.squaredFullPeerRepulsionRadius,
            squaredDistance
        );
    }

    /// <summary>
    /// Adapts the state of the predator regarding its current state and the number boids in its FOV.
    /// </summary>
    /// <param name="nbBoidsNearby">The number of boids in its FOV.</param>
    private void AdaptState(int nbPreysInFOV)
    {
        switch (state)
        {
            case State.CHILLING:
                if (MathHelpers.Bernoulli(predatorsParams.probaHuntingAfterChilling))
                    state = State.HUNTING;
                break;

            case State.HUNTING:
                if (nbPreysInFOV > predatorsParams.nbPreysToAttack)
                    state = State.ATTACKING;
                else if (MathHelpers.Bernoulli(predatorsParams.probaChillingAfterHunting))
                    state = State.CHILLING;
                break;

            case State.ATTACKING:
                if (nbPreysInFOV < predatorsParams.nbPreysToAttack)
                {
                    if (MathHelpers.Bernoulli(predatorsParams.probaHuntingAfterAttacking))
                        state = State.HUNTING;
                    else
                        state = State.CHILLING;
                }
                break;
        }
    }

    /// <summary>
    /// Computes the ideal position of each bone according the entity trajectory history.
    /// Adds wave motion animation to them.
    /// </summary>
    protected override void ComputeBonesPositionsAndRotations()
    {
        base.ComputeBonesPositionsAndRotations();

        AddWaveMotionAnimation();
        SmoothBonesRotations();
    }

    /// <summary>
    /// Adds wave motion animation to the bones.
    /// </summary>
    private void AddWaveMotionAnimation()
    {
        float velocityFactor = 1 +
            predatorsParams.velocityImpactOnWaves *
            (velocity / parameters.velocities[parameters.defaultState] - 1);

        float speed = velocityFactor * predatorsParams.wavesBaseSpeed;
        float magnitude = velocityFactor * predatorsParams.wavesBaseMagnitude;

        currentAnimationPhase += speed % (2 * Mathf.PI);

        for (int boneIndex = parameters.animationFirstBone; boneIndex < bones.Length; boneIndex++)
        {
            (Vector3 bonePosition, Quaternion boneRotation) = bonesPositionsAndRotations[boneIndex];
            Vector3 boneDirection = MathHelpers.RotationToDirection(boneRotation);
            Vector3 right = Vector3.Cross(boneDirection, Vector3.up).normalized;

            float distanceToHead = BoneDistanceToHead(boneIndex);
            Vector3 boneNewPosition = bonePosition + magnitude * Enveloppe(distanceToHead) * Mathf.Sin(
                predatorsParams.wavesBaseSpacialFrequency * distanceToHead - currentAnimationPhase) * right;
            bonesPositionsAndRotations[boneIndex] = (boneNewPosition, boneRotation);
        }
    }

    /// <summary>
    /// Allows to get the correct multiplicator for the wave motion magnitude regarding the distance between a bone 
    /// and the head of the predator.
    /// </summary>
    /// <param name="distanceToHead">Distance between the bone and the head of the predator.</param>
    /// <returns>
    /// The correct multiplicator for the wave motion magnitude.
    /// </returns>
    private float Enveloppe(float distanceToHead)
    {
        return predatorsParams.wavesEnveloppeGradient * distanceToHead + predatorsParams.wavesEnveloppeMin;
    }

    /// <summary>
    /// Adjusts bones rotation to correspond to make it fit their position.
    /// </summary>
    private void SmoothBonesRotations()
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
