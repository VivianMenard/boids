public enum EntityType
{
    BOID,
    PREDATOR
}

public enum State
{
    NORMAL,
    ALONE,
    AFRAID,
    CHILLING,
    HUNTING,
    ATTACKING
}

public enum Behavior
{
    SEPARATION,
    ALIGNMENT,
    COHESION
}

public enum RwState
{
    STRAIGHT_LINE,
    DIRECTION_CHANGE,
    NOT_IN_RW
}

public static class Constants
{
    public const string entitiesManagerTag = "EntitiesManager";

    public const string obstaclesLayerName = "Obstacles";
    public const string boidsLayerName = "Boids";
    public const string predatorsLayerName = "Predators";

    public const string scrollAxisName = "Mouse ScrollWheel";

    public const string fpsDisplayTemplate = "{0} fps";
    public const string nbFishesDisplayTemplate = "{0} fish";
    public const string nbFishesDisplayTemplatePlural = "{0} fishes";
    public const string nbSharksDisplayTemplate = "{0} shark";
    public const string nbSharksDisplayTemplatePlural = "{0} sharks";

    public const string nbBoidsFormat = "#,###";

    public const string shaderTimeOffsetReference = "_timeOffset";
    public const string shaderPauseReference = "_pause";

    public const string godraysNameTemplate = "GodRay_{0}";
}