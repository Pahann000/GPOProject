using UnityEngine;

/// <summary>
/// Запрос на проверку ресурсов. Result заполняется ResourceSystem.
/// </summary>
public class CheckResourcesEvent : IGameEvent
{
    public Player Player;
    public ResourceBundle Cost;
    public bool Result;

    public CheckResourcesEvent(Player player, ResourceBundle cost)
    {
        Player = player;
        Cost = cost;
    }
}

/// <summary>
/// Запрос на списание ресурсов. Result заполняется ResourceSystem.
/// </summary>
public class SpendResourcesEvent : IGameEvent
{
    public Player Player;
    public ResourceBundle Cost;
    public bool Result;

    public SpendResourcesEvent(Player player, ResourceBundle cost)
    {
        Player = player;
        Cost = cost;
    }
}

/// <summary>
/// Уведомление об изменении дерева технологий (для UI)
/// </summary>
public struct TechnologyTreeChangedEvent : IGameEvent
{
    public Player Player;

    public TechnologyTreeChangedEvent(Player player)
    {
        Player = player;
    }
}

/// <summary>
/// Уведомление об исследовании технологии
/// </summary>
public struct TechnologyUnlockedEvent : IGameEvent
{
    public Player Player;
    public TalentData Technology;

    public TechnologyUnlockedEvent(Player player, TalentData technology)
    {
        Player = player;
        Technology = technology;
    }
}