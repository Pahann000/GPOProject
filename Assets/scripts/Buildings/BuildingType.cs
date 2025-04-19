using UnityEngine;
//TODO: Лучше сделать через наследование. сначала базовый класс Building, где будет
//прочность, название, спрайт, и цена строительства.
//А далее подклассы по типу ProductionBuilding(производственные строения) и т.д.
//Тогда нужда в определении кучи параметров уйдёт
//и будет проще изменять логику отдельных типов строений.

/// <summary>
/// Описывает тип постройки и её характеристики.
/// </summary>
[CreateAssetMenu(fileName = "BuildingType", menuName = "Building type")]
public class BuildingType : ScriptableObject
{
    /// <summary>
    /// Наименование типа постройки.
    /// </summary>
    public BuildingTypeName BuildingTypeName;
    /// <summary>
    /// Спрайт постройки.
    /// </summary>
    public Sprite Sprite;

    /// <summary>
    /// Количество металла для постройки.
    /// </summary>
    [Header("Cost")]
    public int MetalCost;
    /// <summary>
    /// Количество минералов для постройки.
    /// </summary>
    public int MineralCost;
    /// <summary>
    /// Количество воды для постройки.
    /// </summary>
    public int WaterCost;

    /// <summary>
    /// Определяет, требует ли постройка плоской поверхности. 
    /// </summary>
    [Header("Requirements")]
    public bool RequiresFlatSurface;
    /// <summary>
    /// Определяет, требует ли постройка доступ к воде. 
    /// </summary>
    public bool RequiresWaterAccess;
    /// <summary>
    /// Определяет, требует ли постройка входные ресурсы. 
    /// </summary>
    public bool RequiresResourceDeposit;
    /// <summary>
    /// Определяет, требует ли постройка энергию. 
    /// </summary>
    public bool RequiresPower;

    /// <summary>
    /// Определяет, производит ли постройка ресурсы.
    /// </summary>
    [Header("Production")]
    public bool ProducesResource;
    /// <summary>
    /// Определяет выходящий ресурс постройки.
    /// </summary>
    // public ResourceType ProducedResourceType;
    /// <summary>
    /// определяет количество выходящих ресурсов
    /// </summary>
    public int ProductionAmount;
    /// <summary>
    /// Определяет скорость производства.
    /// </summary>
    public float ProductionCooldown;

    [Header("Other Effects")]
    public int PopulationIncrease; 
    public int StorageIncrease;    
    public int DefensePower;       
}