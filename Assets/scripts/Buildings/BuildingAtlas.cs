using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Описывает атлас, содержащий префабы всех типов зданий.
/// </summary>
/// <remarks>
/// Раньше атлас хранил три поля (House/MainBuilding/ExtendsBuilding), а
/// <see cref="BuilderSystem"/> выбирал префаб через switch. Из-за этого скрипты
/// Mine, Greenhouse, PowerPlant, Turret, StorageBuilding, LivingModule и
/// ResearchLab нельзя было использовать: их префабы просто некуда было положить.
/// Теперь список открыт для расширения, а каждая запись проверяется по типу.
/// </remarks>
[CreateAssetMenu(fileName = "BuildingAtlas", menuName = "Building atlas")]
public class BuildingAtlas : ScriptableObject
{
    /// <summary>Запись атласа: тип здания и его префаб.</summary>
    [System.Serializable]
    public class BuildingPrefab
    {
        public BuildingTypeNames Type;
        public GameObject Prefab;
    }

    [SerializeField]
    private List<BuildingPrefab> _buildings = new List<BuildingPrefab>();

    /// <summary>Все записи атласа (только чтение).</summary>
    public IReadOnlyList<BuildingPrefab> Buildings => _buildings;

    /// <summary>
    /// Возвращает префаб здания по типу или null, если тип в атласе не описан.
    /// </summary>
    public GameObject GetPrefab(BuildingTypeNames type)
    {
        for (int i = 0; i < _buildings.Count; i++)
        {
            BuildingPrefab entry = _buildings[i];
            if (entry == null || entry.Prefab == null) continue;
            if (entry.Type == type) return entry.Prefab;
        }

        return null;
    }

    /// <summary>
    /// Заполняет атлас (используется редакторскими инструментами и тестами).
    /// </summary>
    public void SetBuildings(IEnumerable<BuildingPrefab> buildings)
    {
        _buildings.Clear();
        if (buildings == null) return;

        foreach (BuildingPrefab entry in buildings)
        {
            if (entry != null) _buildings.Add(entry);
        }
    }
}
