using UnityEngine;

[CreateAssetMenu(fileName = "BuildingType", menuName = "Building type")]
public class BuildingType : ScriptableObject
{
    public BuildingTypeName BuildingTypeName;
    public Sprite Sprite;

    [Header("Cost")]
    public int MetalCost;
    public int MineralCost;
    public int WaterCost;

    [Header("Requirements")]
    public bool RequiresFlatSurface;
    public bool RequiresWaterAccess;
    public bool RequiresResourceDeposit;
    public bool RequiresPower;

    [Header("Production")]
    public bool ProducesResource;
    /// public ResourceType ProducedResourceType;
    public int ProductionAmount;
    public float ProductionCooldown;

    [Header("Other Effects")]
    public int PopulationIncrease; 
    public int StorageIncrease;    
    public int DefensePower;       
}