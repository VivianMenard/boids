/// <summary>
/// Represents the type of an entity.
/// </summary>
public enum EntityType
{
    BOID,
    PREDATOR
}

/// <summary>
/// Represents the state of an entity.
/// </summary>
public enum State
{
    NORMAL,
    ALONE,
    AFRAID,
    CHILLING,
    HUNTING,
    ATTACKING
}

/// <summary>
/// Represents the behavior that an entity will adopt regarding another one.
/// </summary>
public enum Behavior
{
    SEPARATION,
    ALIGNMENT,
    COHESION
}

/// <summary>
/// Represent the random walk state of an entity.
/// </summary>
public enum RwState
{
    STRAIGHT_LINE,
    DIRECTION_CHANGE,
    NOT_IN_RW
}

/// <summary>
/// Static class that regroups all the string constants of the project.
/// </summary>
public static class Constants
{
    public const string entitiesManagerTag = "EntitiesManager";

    public const string obstaclesLayerName = "Obstacles";
    public const string boidsObstaclesLayerName = "BoidsObstacles";
    public const string predatorsObstaclesLayerName = "PredatorsObstacles";
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