using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PredatorScript : EntityScript
{
    private PredatorsParameters predatorsParams;

    protected override void InitParams()
    {
        parameters = entitiesManager.predatorsParams;
        predatorsParams = (PredatorsParameters)parameters;
    }

    protected override void ComputeNewDirection() {}
}
