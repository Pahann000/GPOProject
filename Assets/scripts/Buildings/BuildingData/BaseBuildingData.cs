using UnityEngine;
using System.Collections.Generic;


public abstract class BaseBuildingData : ScriptableObject
{
    [Header("Основные характеристики")]
    public string DisplayName;
    public Sprite Icon;
    public GameObject Prefab;
    public int MaxHealth = 100;
    public int Width = 1;
    public int Height = 1;

    [Header("Стоимость строительства")]
    public ResourceBundle ConstructionCost;

    [Header("Правила размещения")]
    public List<PlacementRule> PlacementRules;
}