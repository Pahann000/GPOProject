using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Чистые правила древа исследований: что можно начать, как идёт прогресс,
/// что открыто и какие бонусы накоплены. Вынесено из <see cref="ResearchSystem"/>,
/// чтобы покрыть логику тестами без сцены и сети.
/// </summary>
public static class ResearchRules
{
    /// <summary>Индекс «исследования нет».</summary>
    public const int NoResearch = -1;

    /// <summary>
    /// Можно ли начать исследование. Причина отказа возвращается для UI и логов.
    /// </summary>
    public static bool CanStart(ResearchProject project, int index, ICollection<int> completed,
        ResearchAtlas atlas, bool hasResources, out string reason)
    {
        reason = string.Empty;

        if (project == null || index < 0)
        {
            reason = "Исследование не найдено.";
            return false;
        }

        if (completed != null && completed.Contains(index))
        {
            reason = "Уже изучено.";
            return false;
        }

        if (!project.ArePrerequisitesMet(completed, atlas))
        {
            reason = "Не выполнены требования.";
            return false;
        }

        if (!hasResources)
        {
            reason = "Недостаточно ресурсов.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Следующее значение прогресса. Не выходит за пределы длительности.
    /// </summary>
    public static float AdvanceProgress(float progress, float duration, float deltaTime, float speed)
    {
        if (duration <= 0f) return 0f;
        if (deltaTime <= 0f || speed <= 0f) return Mathf.Clamp(progress, 0f, duration);

        return Mathf.Clamp(progress + deltaTime * speed, 0f, duration);
    }

    /// <summary>
    /// Множитель скорости исследований: база 100% плюс бонусы от изученных
    /// технологий и от действующих лабораторий.
    /// </summary>
    public static float ComputeSpeed(float researchBonusPercent, int labs, float labSpeedPercent)
    {
        float speed = 1f + Mathf.Max(0f, researchBonusPercent) / 100f;

        if (labs > 0 && labSpeedPercent > 0f)
        {
            speed += labs * labSpeedPercent / 100f;
        }

        return Mathf.Max(0.1f, speed);
    }

    /// <summary>Доля прогресса исследования (0..1).</summary>
    public static float Progress01(float progress, float duration)
    {
        if (duration <= 0f) return 0f;

        return Mathf.Clamp01(progress / duration);
    }

    /// <summary>
    /// Доступно ли здание: либо в стартовом наборе, либо открыто изученным исследованием.
    /// </summary>
    public static bool IsBuildingUnlocked(ResearchAtlas atlas, ICollection<int> completed, BuildingTypeNames type)
    {
        if (atlas == null) return true;               // нет атласа — не блокируем игру
        if (atlas.IsDefaultBuilding(type)) return true;
        if (completed == null || completed.Count == 0) return false;

        for (int i = 0; i < atlas.Count; i++)
        {
            if (!completed.Contains(i)) continue;

            ResearchProject project = atlas.Get(i);
            if (project == null || project.unlockBuildings == null) continue;

            for (int b = 0; b < project.unlockBuildings.Length; b++)
            {
                if (project.unlockBuildings[b] == type) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Доступен ли тип юнита: стартовый набор или изученное исследование.
    /// </summary>
    public static bool IsUnitUnlocked(ResearchAtlas atlas, ICollection<int> completed, UnitTypeName type)
    {
        if (atlas == null) return true;
        if (atlas.IsDefaultUnit(type)) return true;
        if (completed == null || completed.Count == 0) return false;

        for (int i = 0; i < atlas.Count; i++)
        {
            if (!completed.Contains(i)) continue;

            ResearchProject project = atlas.Get(i);
            if (project == null || project.unlockUnits == null) continue;

            for (int u = 0; u < project.unlockUnits.Length; u++)
            {
                if (project.unlockUnits[u] == type) return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Суммарный бонус по всем изученным исследованиям (в процентах).
    /// </summary>
    public static float SumBonusPercent(ResearchAtlas atlas, ICollection<int> completed,
        Func<ResearchProject, float> select)
    {
        if (atlas == null || completed == null || completed.Count == 0 || select == null) return 0f;

        float total = 0f;

        for (int i = 0; i < atlas.Count; i++)
        {
            if (!completed.Contains(i)) continue;

            ResearchProject project = atlas.Get(i);
            if (project == null) continue;

            total += select(project);
        }

        return total;
    }

    /// <summary>Стоимость исследования в виде массива (null-safe).</summary>
    public static ResourcePair[] CostOf(ResearchProject project)
    {
        if (project == null || project.cost == null || project.cost.Length == 0) return null;

        return project.cost;
    }
}
