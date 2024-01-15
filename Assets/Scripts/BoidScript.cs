using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidScript: EntityScript
{
    private enum Rule {
        SEPARATION,
        ALIGNMENT,
        COHESION
    }

    private BoidsParameters boidsParams;

    protected override void InitParams() {
        parameters = entitiesManager.boidsParams;
        boidsParams = (BoidsParameters)parameters;
    }

    protected override void ComputeNewDirection() {
        Collider[] collidersNearby = Physics.OverlapSphere(
            transform.position, 
            visionDistance
        );

        Vector3 separationPositionSum = Vector3.zero,
            alignmentDirectionSum = Vector3.zero,
            cohesionPositionSum = Vector3.zero;

        int nbBoidsSeparation = 0,
            nbBoidsAlignment = 0,
            nbBoidsCohesion = 0;

        foreach (Collider collider in collidersNearby) {
            if (!IsAVisibleBoid(collider))
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

        AdaptVisionDistance(nbBoidsSeparation + nbBoidsAlignment + nbBoidsCohesion);

        Vector3 separationDirection = ComputeDirectionForRule(
                Rule.SEPARATION, separationPositionSum, nbBoidsSeparation),
            alignmentDirection = ComputeDirectionForRule(
                Rule.ALIGNMENT, alignmentDirectionSum, nbBoidsAlignment),
            cohesionDirection = ComputeDirectionForRule(
                Rule.COHESION, cohesionPositionSum, nbBoidsCohesion);

        float separationCoeff = ComputeRuleCoeff(
                nbBoidsSeparation, boidsParams.separationStrengh),
            alignmentCoeff = ComputeRuleCoeff(
                nbBoidsAlignment, boidsParams.alignmentStrengh),
            cohesionCoeff = ComputeRuleCoeff(
                nbBoidsCohesion, boidsParams.cohesionStrengh);

        Vector3 newDirection = (
            (
                boidsParams.momentumStrengh * Direction + 
                separationCoeff * separationDirection +
                alignmentCoeff * alignmentDirection + 
                cohesionCoeff * cohesionDirection
            ) / (
                boidsParams.momentumStrengh + 
                separationCoeff +
                alignmentCoeff + 
                cohesionCoeff
            )
        ).normalized;

        SetDirection(newDirection);
    }

    private Vector3 ComputeDirectionForRule(Rule rule, Vector3 relevantSum, int nbInvolvedBoids) {
        if (nbInvolvedBoids == 0)
            return Vector3.zero;
        
        if (rule == Rule.ALIGNMENT) {
            Vector3 averageDirection = relevantSum.normalized;
            return averageDirection;
        }

        Vector3 averagePosition = relevantSum / (float)nbInvolvedBoids;
        Vector3 directionToAveragePosition = (averagePosition - transform.position).normalized;

        if (rule == Rule.SEPARATION)
            return -directionToAveragePosition;
        
        return directionToAveragePosition;
    }

    private float ComputeRuleCoeff(int nbInvolvedBoids, float ruleStrengh) {
        return (nbInvolvedBoids == 0) ? 0 : ruleStrengh;
    }

    private void AdaptVisionDistance(int nbBoidsInFOV) {
        if (nbBoidsInFOV > boidsParams.idealNbNeighbors 
            && visionDistance > 1){
            visionDistance--;
        } else if (nbBoidsInFOV < boidsParams.idealNbNeighbors 
            && visionDistance < boidsParams.visionDistance) {
            visionDistance++;
        }
    }
}
