using UnityEngine;

public static class Accessors
{
    private static EntitiesManagerScript entitiesManager;

    public static EntitiesManagerScript EntitiesManager
    {
        get
        {
            if (entitiesManager == null)
                entitiesManager = GameObject.FindGameObjectWithTag(Constants.entitiesManagerTag).
            GetComponent<EntitiesManagerScript>();

            return entitiesManager;
        }
    }
}