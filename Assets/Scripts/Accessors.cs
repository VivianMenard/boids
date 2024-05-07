using System;
using UnityEngine;

public static class Accessors
{
    private static EntitiesManagerScript entitiesManager;

    public static EntitiesManagerScript EntitiesManager
    {
        get
        {
            if (entitiesManager == null)
            {
                GameObject entitiesManagerGO = GameObject.
                    FindGameObjectWithTag(Constants.entitiesManagerTag);

                if (entitiesManagerGO == null)
                    throw new Exception(
                        "No object found with tag: " + Constants.entitiesManagerTag);

                entitiesManager = entitiesManagerGO.GetComponent<EntitiesManagerScript>();

                if (entitiesManager == null)
                    throw new Exception(
                        "No EntitiesManagerScript component on entitiesManager GameObject.");
            }

            return entitiesManager;
        }
    }
}