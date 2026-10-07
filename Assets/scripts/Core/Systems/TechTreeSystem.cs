using System.Collections.Generic;
using UnityEngine;

public class TechTreeSystem : IGameSystem
{
    public string SystemName => nameof(TechTreeSystem);
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private List<TalentData> _allTechnologies;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        // Загружаем технологии из Resources
        _allTechnologies = new List<TalentData>(
            Resources.LoadAll<TalentData>("Technologies")
        );

        Debug.Log($"[{SystemName}] Загружено технологий: {_allTechnologies.Count}");
    }

    public void Tick(float deltaTime) { }
    public void FixedTick(float deltaTime) { }
    public void Shutdown() { }

    /// <summary>
    /// Проверка: можно ли исследовать технологию для игрока
    /// </summary>
    public bool CanUnlock(Player player, TalentData tech)
    {
        if (tech == null || tech.IsUnlocked) return false;
        if (player == null) return false;

        // Проверка зависимостей
        if (tech.Prerequisites != null)
        {
            foreach (var prereq in tech.Prerequisites)
            {
                if (prereq == null || !prereq.IsUnlocked) return false;
            }
        }

        // Проверка ресурсов через событие
        var checkEvent = new CheckResourcesEvent(player, tech.Cost);
        _kernel.EventBus.Raise(checkEvent);

        return checkEvent.Result;
    }

    /// <summary>
    /// Исследовать технологию
    /// </summary>
    public bool UnlockTechnology(Player player, TalentData tech)
    {
        if (!CanUnlock(player, tech)) return false;

        // Списываем ресурсы через событие
        var spendEvent = new SpendResourcesEvent(player, tech.Cost);
        _kernel.EventBus.Raise(spendEvent);

        if (!spendEvent.Result)
        {
            Debug.LogWarning($"[{SystemName}] Не удалось списать ресурсы для {tech.TalentName}");
            return false;
        }

        // Разблокируем технологию
        tech.IsUnlocked = true;

        // Уведомляем UI
        _kernel.EventBus.Raise(new TechnologyUnlockedEvent(player, tech));
        _kernel.EventBus.Raise(new TechnologyTreeChangedEvent(player));

        Debug.Log($"[{SystemName}] Игрок {player.netId} исследовал: {tech.TalentName}");
        return true;
    }

    public List<TalentData> GetAllTechnologies() => _allTechnologies;

    public List<TalentData> GetUnlockedTechnologies() =>
        _allTechnologies.FindAll(t => t.IsUnlocked);
}