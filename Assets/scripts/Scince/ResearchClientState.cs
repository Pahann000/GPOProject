using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Клиентская копия состояния исследований локального игрока.
/// </summary>
/// <remarks>
/// UI не имеет доступа к серверной <see cref="ResearchSystem"/>, поэтому снимок,
/// присланный <see cref="ResearchSync"/>, складывается сюда. Проверки «открыто ли
/// здание/юнит» и прогресс читаются отсюда и работают одинаково в Host и на
/// выделенном клиенте.
/// </remarks>
public static class ResearchClientState
{
    private static readonly HashSet<int> _completed = new HashSet<int>();

    /// <summary>Индекс текущего исследования или <see cref="ResearchRules.NoResearch"/>.</summary>
    public static int CurrentIndex { get; private set; } = ResearchRules.NoResearch;

    /// <summary>Прогресс текущего исследования в секундах.</summary>
    public static float Progress { get; private set; }

    /// <summary>Длительность текущего исследования в секундах.</summary>
    public static float Duration { get; private set; }

    /// <summary>Игрок, чьё состояние хранится.</summary>
    public static Player Player { get; private set; }

    /// <summary>Растёт при каждом обновлении снимка — удобно для UI.</summary>
    public static int Revision { get; private set; }

    /// <summary>Изученные исследования (индексы в атласе).</summary>
    public static ICollection<int> Completed => _completed;

    /// <summary>Доля прогресса текущего исследования (0..1).</summary>
    public static float Progress01 => ResearchRules.Progress01(Progress, Duration);

    /// <summary>Изучено ли исследование по индексу.</summary>
    public static bool IsCompleted(int index)
    {
        return index >= 0 && _completed.Contains(index);
    }

    /// <summary>Доступно ли здание локальному игроку.</summary>
    public static bool IsBuildingUnlocked(BuildingTypeNames type)
    {
        return ResearchRules.IsBuildingUnlocked(ResearchAtlas.Cached, _completed, type);
    }

    /// <summary>Доступен ли тип юнита локальному игроку.</summary>
    public static bool IsUnitUnlocked(UnitTypeName type)
    {
        return ResearchRules.IsUnitUnlocked(ResearchAtlas.Cached, _completed, type);
    }

    /// <summary>
    /// Применяет снимок с сервера. Повторный вызов с теми же данными безопасен.
    /// </summary>
    public static void Apply(Player player, int currentIndex, float progress, float duration, int[] completed)
    {
        Player = player;
        CurrentIndex = currentIndex;
        Progress = Mathf.Max(0f, progress);
        Duration = Mathf.Max(0f, duration);

        _completed.Clear();
        if (completed != null)
        {
            for (int i = 0; i < completed.Length; i++) _completed.Add(completed[i]);
        }

        Revision++;
    }

    /// <summary>Сбрасывает состояние (выход из игры, смена игрока).</summary>
    public static void Clear()
    {
        _completed.Clear();
        CurrentIndex = ResearchRules.NoResearch;
        Progress = 0f;
        Duration = 0f;
        Player = null;
        Revision++;
    }
}
