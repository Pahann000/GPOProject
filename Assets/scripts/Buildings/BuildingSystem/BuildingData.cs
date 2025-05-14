using UnityEngine;
using System.Collections.Generic;

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
    public ResourceBundle ConstructionCost;

    [Tooltip("Правила размещения здания на карте")]
    public List<PlacementRule> PlacementRules;

    [Tooltip("Максимальное здоровье зданий")]
    public int MaxHealth;

    [Tooltip("Ширина здания в клетках")]
    public int Width = 1;

    [Tooltip("Высота здания в клетках")]
    public int Height = 1;

    [Header("Production Settings")]
    public ResourceBundle InputResources;
    public ResourceBundle OutputResources;
}