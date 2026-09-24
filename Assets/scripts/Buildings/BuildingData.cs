using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    public ResourcePair[] ConstructionCost;
    public BuildingTypeNames BuildingTypeName;
    //public List<PlacementRule> PlacementRules;
    public int MaxHealth;
    public int Width = 1;
    public int Height = 1;


    [Header("Production Settings")]
    public ResourcePair[] InputResources;
    public ResourcePair[] OutputResources;
}