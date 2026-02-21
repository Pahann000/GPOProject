using UnityEngine;

public struct BuildingSelectedEvent : IGameEvent
{
    public Building SelectedBuilding;

    public BuildingSelectedEvent(Building building)
    {
        SelectedBuilding = building;
    }
}

public struct BuildingDeselectedEvent : IGameEvent { }