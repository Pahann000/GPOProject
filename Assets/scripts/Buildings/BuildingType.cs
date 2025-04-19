using UnityEngine;
//TODO: ����� ������� ����� ������������. ������� ������� ����� Building, ��� �����
//���������, ��������, ������, � ���� �������������.
//� ����� ��������� �� ���� ProductionBuilding(���������������� ��������) � �.�.
//����� ����� � ����������� ���� ���������� ����
//� ����� ����� �������� ������ ��������� ����� ��������.

/// <summary>
/// ��������� ��� ��������� � � ��������������.
/// </summary>
[CreateAssetMenu(fileName = "BuildingType", menuName = "Building type")]
public class BuildingType : ScriptableObject
{
    /// <summary>
    /// ������������ ���� ���������.
    /// </summary>
    public BuildingTypeName BuildingTypeName;
    /// <summary>
    /// ������ ���������.
    /// </summary>
    public Sprite Sprite;

    /// <summary>
    /// ���������� ������� ��� ���������.
    /// </summary>
    [Header("Cost")]
    public int MetalCost;
    /// <summary>
    /// ���������� ��������� ��� ���������.
    /// </summary>
    public int MineralCost;
    /// <summary>
    /// ���������� ���� ��� ���������.
    /// </summary>
    public int WaterCost;

    /// <summary>
    /// ����������, ������� �� ��������� ������� �����������. 
    /// </summary>
    [Header("Requirements")]
    public bool RequiresFlatSurface;
    /// <summary>
    /// ����������, ������� �� ��������� ������ � ����. 
    /// </summary>
    public bool RequiresWaterAccess;
    /// <summary>
    /// ����������, ������� �� ��������� ������� �������. 
    /// </summary>
    public bool RequiresResourceDeposit;
    /// <summary>
    /// ����������, ������� �� ��������� �������. 
    /// </summary>
    public bool RequiresPower;

    /// <summary>
    /// ����������, ���������� �� ��������� �������.
    /// </summary>
    [Header("Production")]
    public bool ProducesResource;
    /// <summary>
    /// ���������� ��������� ������ ���������.
    /// </summary>
    // public ResourceType ProducedResourceType;
    /// <summary>
    /// ���������� ���������� ��������� ��������
    /// </summary>
    public int ProductionAmount;
    /// <summary>
    /// ���������� �������� ������������.
    /// </summary>
    public float ProductionCooldown;

    [Header("Other Effects")]
    public int PopulationIncrease; 
    public int StorageIncrease;    
    public int DefensePower;       
}