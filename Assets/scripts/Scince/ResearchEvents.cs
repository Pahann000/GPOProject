using UnityEngine;

/// <summary>
/// Событие: игрок завершил исследование. Поднимается на сервере.
/// </summary>
public struct ResearchCompletedEvent : IGameEvent
{
    public Player Player;
    public ResearchProject Project;

    public ResearchCompletedEvent(Player player, ResearchProject project)
    {
        Player = player;
        Project = project;
    }
}

/// <summary>
/// Событие: игрок начал исследование. Поднимается на сервере.
/// </summary>
public struct ResearchStartedEvent : IGameEvent
{
    public Player Player;
    public ResearchProject Project;

    public ResearchStartedEvent(Player player, ResearchProject project)
    {
        Player = player;
        Project = project;
    }
}
