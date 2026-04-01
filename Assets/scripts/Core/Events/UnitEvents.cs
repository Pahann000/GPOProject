using UnityEngine;

/// <summary>
/// Событие создания нового юнита.
/// </summary>
public struct UnitSpawnedEvent : IGameEvent
{
    public Unit UnitInstance;
    public UnitSpawnedEvent(Unit unit) => UnitInstance = unit;
}

/// <summary>
/// Событие приказа (например, копать или атаковать).
/// </summary>
public struct UnitCommandEvent : IGameEvent
{
    public IDamagable Target;
    public Vector2 Position;
    public Player Owner;

    public UnitCommandEvent(IDamagable target, Vector2 position, Player owner)
    {
        Target = target;
        Position = position;
        Owner = owner;
    }
}