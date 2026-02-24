using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Класс управления исследованиями
/// </summary>
public class TalentManager : MonoBehaviour
{
    // Синглтон
    public static TalentManager Instance { get; private set; }

    // Все технологии в игре
    [Header("Настройки")]
    [SerializeField] private List<TalentData> allTechnologies;

    // Ссылка на ResourceManager
    [Header("Ссылка на существующий менеджер")]
    [SerializeField] private ResourceManager resourceManager;

    // События
    public System.Action<TalentData> OnTechnologyResearched;

    public System.Action OnTechnologyTreeUpdated;

    // Кэшированные данные
    private Dictionary<string, TalentData> technologyDict;
    private List<TalentData> availableTechnologies;
    private List<TalentData> researchedTechnologies;

    // Синглтон
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeTechnologySystem();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
  
    }

    /// <summary>
    /// Происходит при разрушении объекта.
    /// </summary>
    private void OnDestroy()
    {
        // Отписываемся от событий
        if (resourceManager != null)
        {
            ResourceManager.OnResourceChanged -= OnResourceChanged;
        }
    }

    /// <summary>
    /// Инициализация системы
    /// </summary>
    private void InitializeTechnologySystem()
    {
        technologyDict = new Dictionary<string, TalentData>();
        availableTechnologies = new List<TalentData>();
        researchedTechnologies = new List<TalentData>();

        // Заполняем словарь для быстрого доступа
        foreach (var tech in allTechnologies)
        {
            if (tech != null && !technologyDict.ContainsKey(tech.name))
            {
                technologyDict[tech.name] = tech;
                tech.IsResearched = false;
                tech.IsAvailable = false;
            }
        }
    }

    /// <summary>
    /// Вызывается при изменении ресурсов - обновляем доступность технологий
    /// </summary>
    private void OnResourceChanged(ResourceType type)
    {
        UpdateAllTechnologiesAvailability();
    }

    /// <summary>
    /// Обновляет доступность всех технологий на основе выполненных зависимостей
    /// </summary>
    private void UpdateAllTechnologiesAvailability()
    {
        bool anyChanged = false;

        foreach (var tech in allTechnologies)
        {
            if (tech == null || tech.IsResearched)
                continue;

            bool wasAvailable = tech.IsAvailable;
            bool allPrerequisitesResearched = true;

            // Проверяем все зависимости
            foreach (var prerequisite in tech.Prerequisites)
            {
                if (prerequisite == null || !prerequisite.IsResearched)
                {
                    allPrerequisitesResearched = false;
                    break;
                }
            }

            tech.IsAvailable = allPrerequisitesResearched;

            // Обновляем список доступных
            if (tech.IsAvailable && !availableTechnologies.Contains(tech))
            {
                availableTechnologies.Add(tech);
                anyChanged = true;
            }
            else if (!tech.IsAvailable && availableTechnologies.Contains(tech))
            {
                availableTechnologies.Remove(tech);
                anyChanged = true;
            }

            if (wasAvailable != tech.IsAvailable)
                anyChanged = true;
        }

        if (anyChanged)
        {
            OnTechnologyTreeUpdated?.Invoke();
        }
    }

    /// <summary>
    /// Проверяет, можно ли исследовать технологию
    /// </summary>
    public bool CanResearch(TalentData technology)
    {
        if (technology == null)
            return false;

        if (technology.IsResearched)
            return false;

        if (!technology.IsAvailable)
            return false;

        // Используем существующий метод ResourceManager для проверки ресурсов
        return resourceManager != null && resourceManager.HasResources(technology.ResearchCost);
    }

    /// <summary>
    /// Пытается исследовать технологию
    /// </summary>
    public bool ResearchTechnology(TalentData technology)
    {
        if (!CanResearch(technology))
            return false;

        // Используем существующий метод ResourceManager для списания ресурсов
        bool spent = resourceManager.TrySpendResources(technology.ResearchCost);

        if (!spent)
            return false;

        // Отмечаем как исследованное
        technology.IsResearched = true;
        researchedTechnologies.Add(technology);

        if (availableTechnologies.Contains(technology))
            availableTechnologies.Remove(technology);

        // Применяем эффекты технологии
        ApplyTechnologyEffects(technology);

        // Обновляем доступность остальных технологий
        UpdateAllTechnologiesAvailability();

        // Вызываем событие
        OnTechnologyResearched?.Invoke(technology);
        OnTechnologyTreeUpdated?.Invoke();

        Debug.Log($"Технология исследована: {technology.TalentName}");
        return true;
    }

    /// <summary>
    /// Применяет все эффекты технологии
    /// </summary>
    private void ApplyTechnologyEffects(TalentData technology)
    {
        if (technology.UnlockEffects == null)
            return;

        foreach (var effect in technology.UnlockEffects)
        {
            if (effect != null)
            {
                effect.Apply();
                Debug.Log($"Применен эффект: {effect.GetEffectName()} от технологии {technology.TalentName}");
            }
        }
    }

    /// <summary>
    /// Получает прогресс исследования (сколько ресурсов уже собрано)
    /// </summary>
    public float GetResearchProgress(TalentData technology)
    {
        if (technology == null || technology.IsResearched)
            return technology != null && technology.IsResearched ? 1f : 0f;

        if (!technology.IsAvailable)
            return 0f;

        if (technology.ResearchCost.Resources == null || technology.ResearchCost.Resources.Count == 0)
            return 0f;

        float totalCost = 0f;
        float collected = 0f;

        foreach (var cost in technology.ResearchCost.Resources)
        {
            totalCost += cost.Amount;
            int currentAmount = resourceManager != null ?
                resourceManager.GetResource(cost.Type) : 0;
            collected += Mathf.Min(cost.Amount, currentAmount);
        }

        return totalCost > 0 ? collected / totalCost : 0f;
    }

}
