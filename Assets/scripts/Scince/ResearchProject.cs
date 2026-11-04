using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Исследование в древе технологий: цена в ресурсах, время изучения,
/// требования и эффекты (разблокировка зданий/юнитов, бонусы).
/// </summary>
/// <remarks>
/// Раньше у проекта было только имя и «научные очки», а состояние завершённости
/// хранилось в самом ассете (<c>IsCompleted</c>). Ассет общий для всех игроков
/// и сессий, поэтому так хранить прогресс нельзя: теперь состояние живёт в
/// <see cref="ResearchSystem"/> на сервере, а ассет описывает только правила.
/// </remarks>
[CreateAssetMenu(fileName = "ResearchProject", menuName = "Research/Project")]
public class ResearchProject : ScriptableObject
{
    [Header("Описание")]
    public string projectName;

    [TextArea(2, 6)]
    public string description;

    [Tooltip("Иконка для UI.")]
    public Sprite icon;

    [Header("Стоимость и время")]
    [Tooltip("Ресурсы, которые списываются при старте исследования.")]
    public ResourcePair[] cost;

    [Tooltip("Сколько секунд идёт исследование при скорости 100%.")]
    public float durationSeconds = 30f;

    [Header("Требования")]
    [Tooltip("Исследования, которые должны быть завершены раньше.")]
    public List<ResearchProject> requiredPrerequisites = new List<ResearchProject>();

    [Header("Что открывает")]
    [Tooltip("Типы зданий, доступные после изучения.")]
    public BuildingTypeNames[] unlockBuildings;

    [Tooltip("Типы юнитов, доступные после изучения.")]
    public UnitTypeName[] unlockUnits;

    [Header("Улучшения")]
    [Tooltip("Прибавка к производству зданий владельца, %.")]
    public float productionBonusPercent;

    [Tooltip("Прибавка к урону юнитов владельца, %.")]
    public float unitDamageBonusPercent;

    [Tooltip("Прибавка к здоровью новых юнитов владельца, %.")]
    public float unitHealthBonusPercent;

    [Tooltip("Прибавка к скорости исследований владельца, %.")]
    public float researchSpeedBonusPercent;

    /// <summary>Отображаемое имя исследования (с запасным вариантом).</summary>
    public string DisplayName => string.IsNullOrEmpty(projectName) ? name : projectName;

    /// <summary>Длительность исследования с защитой от нуля и отрицательных значений.</summary>
    public float Duration => Mathf.Max(0.1f, durationSeconds);

    /// <summary>
    /// Проверяет, что все требования выполнены.
    /// </summary>
    /// <param name="completed">Индексы уже завершённых исследований в атласе.</param>
    /// <param name="atlas">Атлас, по которому определяются индексы требований.</param>
    public bool ArePrerequisitesMet(ICollection<int> completed, ResearchAtlas atlas)
    {
        if (atlas == null) return true;
        if (requiredPrerequisites == null || requiredPrerequisites.Count == 0) return true;
        if (completed == null) return false;

        for (int i = 0; i < requiredPrerequisites.Count; i++)
        {
            ResearchProject prerequisite = requiredPrerequisites[i];
            if (prerequisite == null) continue;

            int index = atlas.IndexOf(prerequisite);
            if (index < 0) continue;          // требование не входит в атлас — игнорируем
            if (!completed.Contains(index)) return false;
        }

        return true;
    }
}
