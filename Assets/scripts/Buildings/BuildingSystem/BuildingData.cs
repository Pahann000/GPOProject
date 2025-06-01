using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    public GameObject Prefab;
    public ResourceBundle ConstructionCost;
    public List<PlacementRule> PlacementRules;
    public int MaxHealth;
    public int Width = 1;
    public int Height = 1;

    [Header("Production Settings")]
    public ResourceBundle InputResources;
    public ResourceBundle OutputResources;
}