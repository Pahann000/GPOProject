using UnityEngine;

/// <summary>
/// ��������� �����, ���������� ��� ���� ��������. 
/// </summary>
[CreateAssetMenu(fileName = "BuildingAtlas", menuName = "Building atlas")]
public class BuildingAtlas : ScriptableObject
{
    public BuildingType LivingModule;
    public BuildingType Storage;
    public BuildingType PowerPlant;
    public BuildingType Greenhouse;
    public BuildingType Mine;
    public BuildingType ResearchLab;
    public BuildingType Turret;
}