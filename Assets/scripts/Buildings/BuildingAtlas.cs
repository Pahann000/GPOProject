using UnityEngine;

/// <summary>
/// Описывает атлас, содержащий типы зданий.
/// </summary>
[CreateAssetMenu(fileName = "BuildingAtlas", menuName = "Building atlas")]
public class BuildingAtlas : ScriptableObject
{
    /// <summary>
    /// Дом.
    /// </summary>
    public GameObject House;

    /// <summary>Главное здание фракции: пассивный доход и склады.</summary>
    public GameObject MainBuilding;

    /// <summary>Здание-бонус: расширяет лимит хранения ресурса.</summary>
    public GameObject ExtendsBuilding;
}
