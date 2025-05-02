using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Данные здания, создаваемые как ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    [Tooltip("Название для отображения в UI")]
    public string DisplayName;

    [Tooltip("Иконка для отображения в UI")]
    public Sprite Icon;

    [Tooltip("Префаб здания для инстанцирования")]
    public GameObject Prefab;

    [Tooltip("Ресурсы, необходимые для постройки")]
    public ResourceCost ConstructionCost;

    [Tooltip("Правила размещения здания на карте")]
    public List<PlacementRule> PlacementRules;
}

/// <summary>
/// Структура, описывающая стоимость постройки в ресурсах
/// </summary>
[System.Serializable]
public struct ResourceCost
{
    [Tooltip("Количество металла")]
    public int Metal;

    [Tooltip("Количество минералов")]
    public int Minerals;

    [Tooltip("Количество воды")]
    public int Water;
}

