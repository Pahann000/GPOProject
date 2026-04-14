using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Класс управления исследованиями
/// </summary>
public class TechTreeManager : MonoBehaviour
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

    /// <summary>
    /// Проверяет можно ли изучить технологию.
    /// </summary>
    /// <param name="tech"></param>
    /// <returns></returns>
    public bool CanUnlock(TalentData tech)
    {
        if (tech == null || tech.IsUnlocked) return false;

        if (tech.Prerequisites[0] != null)
        {
            foreach (var prereq in tech.Prerequisites)
            {
                if (prereq == null || !prereq.IsUnlocked) return false;
            }
        }

        if (tech.HasCost)
        {
            if (!resourceManager.HasResources(tech.Cost))
            {
                return false;
            }
        }

        return true;
    }

    public bool UnlockTechnology(TalentData tech)
    {
        if (!CanUnlock(tech)) return false;

        if (tech.HasCost)
        {
            bool success = resourceManager.TrySpendResources(tech.Cost);
            if (!success)
            {
                Debug.LogWarning($"Failed to spend resources for {tech.TalentName}");
                return false;
            }
        }

        // Отмечаем как исследованную
        tech.IsUnlocked = true;

        // Уведомляем
        OnTechnologyUnlocked?.Invoke();
        OnTechnologyTreeChanged?.Invoke();

        Debug.Log($"Технология исследована: {tech.TalentName}");
        return true;
    }

    /// <summary>
    /// Возвращает разблокированные технологии.
    /// </summary>
    /// <returns></returns>
    public List<TalentData> GetUnlockedTechnologies()
    {
        return allTechnologies.Where(t => t.IsUnlocked).ToList();
    }

    /// <summary>
    /// Возвращает доступные технологии.
    /// </summary>
    /// <returns></returns>
    public List<TalentData> GetAvailableTechnologies()
    {
        return allTechnologies.Where(t => !t.IsUnlocked && CanUnlock(t)).ToList();
    }
}
