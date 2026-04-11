using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Класс управления исследованиями
/// </summary>
public class TalentManager : MonoBehaviour
{
    [Header("Настройки")]
    public List<TalentData> allTechnologies;
    public ResourceManager resourceManager;

    // События для UI
    public System.Action OnTechnologyUnlocked;
    public System.Action OnTechnologyTreeChanged;

    private void Start()
    {
        // Подписываемся на изменения ресурсов
        if (resourceManager != null)
        {
            ResourceManager.OnResourceChanged += OnResourceChanged;
        }
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
        {
            ResourceManager.OnResourceChanged -= OnResourceChanged;
        }
    }

    private void OnResourceChanged(ResourceType type)
    {
        OnTechnologyTreeChanged?.Invoke();
    }

    public bool CanUnlock(TalentData tech)
    {
        if (tech == null || tech.IsUnlocked) return false;

        // Проверяем зависимости
        foreach (var prereq in tech.Prerequisites)
        {
            if (prereq == null || !prereq.IsUnlocked) return false;
        }

        // Проверяем ресурсы
        foreach (var cost in tech.cost)
        {
            if (resourceManager.GetResource(cost.type) < cost.amount) return false;
        }

        return true;
    }

    public bool UnlockTechnology(TalentData tech)
    {
        if (!CanUnlock(tech)) return false;

        // Списываем ресурсы
        foreach (var cost in tech.cost)
        {
            resourceManager.TrySpendResource(cost.type, cost.amount);
        }

        // Отмечаем как исследованную
        tech.IsUnlocked = true;

        // Уведомляем
        OnTechnologyUnlocked?.Invoke();
        OnTechnologyTreeChanged?.Invoke();

        Debug.Log($"Технология исследована: {tech.TalentName}");
        return true;
    }

    public List<TalentData> GetUnlockedTechnologies()
    {
        return allTechnologies.Where(t => t.IsUnlocked).ToList();
    }

    public List<TalentData> GetAvailableTechnologies()
    {
        return allTechnologies.Where(t => !t.IsUnlocked && CanUnlock(t)).ToList();
    }
}
