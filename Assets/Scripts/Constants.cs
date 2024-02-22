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