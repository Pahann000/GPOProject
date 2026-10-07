public class BuildingPlacedEvent : IGameEvent
{
    BuildingData _selectedBuilding;
    Player player;

    public BuildingPlacedEvent (BuildingData selectedBuilding, Player player)
    {
        _selectedBuilding = selectedBuilding;
        this.player = player;
    }
}

public class BuildingStartPlacingEvent : IGameEvent
{
    public BuildingData _selectedBuilding;
    Player player;

    public BuildingStartPlacingEvent(BuildingData selectedBuilding, Player player)
    {
        _selectedBuilding = selectedBuilding;
        this.player = player;
    }
}
