using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Атлас древа исследований: упорядоченный список исследований и набор того,
/// что доступно фракции без исследований.
/// </summary>
/// <remarks>Ассет лежит в Resources, чтобы и сервер, и клиент получали его без
/// ссылок в сцене или префабах (<see cref="Cached"/>).</remarks>
[CreateAssetMenu(fileName = "ResearchAtlas", menuName = "Research/Atlas")]
public class ResearchAtlas : ScriptableObject
{
    /// <summary>Имя ассета в папке Resources.</summary>
    public const string ResourceName = "ResearchAtlas";

    [Tooltip("Все исследования в порядке отображения (сначала базовые).")]
    public List<ResearchProject> projects = new List<ResearchProject>();

    [Header("Доступно без исследований")]
    public BuildingTypeNames[] defaultBuildings = new BuildingTypeNames[0];
    public UnitTypeName[] defaultUnits = new UnitTypeName[0];

    private static ResearchAtlas _cached;

    /// <summary>Общий атлас из Resources (с кэшированием). Может быть null.</summary>
    public static ResearchAtlas Cached
    {
        get
        {
            if (_cached == null) _cached = Resources.Load<ResearchAtlas>(ResourceName);
            return _cached;
        }
    }

    /// <summary>Сбрасывает кэш (нужно после подмены ассета в тестах).</summary>
    public static void ResetCache()
    {
        _cached = null;
    }

    /// <summary>Количество исследований в атласе.</summary>
    public int Count => projects != null ? projects.Count : 0;

    /// <summary>Исследование по индексу или null.</summary>
    public ResearchProject Get(int index)
    {
        if (projects == null || index < 0 || index >= projects.Count) return null;
        return projects[index];
    }

    /// <summary>Индекс исследования в атласе или -1.</summary>
    public int IndexOf(ResearchProject project)
    {
        if (project == null || projects == null) return -1;
        return projects.IndexOf(project);
    }

    /// <summary>Доступно ли здание без исследований.</summary>
    public bool IsDefaultBuilding(BuildingTypeNames type)
    {
        if (defaultBuildings == null) return false;

        for (int i = 0; i < defaultBuildings.Length; i++)
        {
            if (defaultBuildings[i] == type) return true;
        }

        return false;
    }

    /// <summary>Доступен ли юнит без исследований.</summary>
    public bool IsDefaultUnit(UnitTypeName type)
    {
        if (defaultUnits == null) return false;

        for (int i = 0; i < defaultUnits.Length; i++)
        {
            if (defaultUnits[i] == type) return true;
        }

        return false;
    }
}
