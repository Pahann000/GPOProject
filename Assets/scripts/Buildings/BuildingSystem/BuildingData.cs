using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    [Tooltip("�������� ��� ����������� � UI")]
    public string DisplayName;

    [Tooltip("������ ��� ����������� � UI")]
    public Sprite Icon;

    [Tooltip("������ ������ ��� ���������������")]
    public GameObject Prefab;

    [Tooltip("�������, ����������� ��� ���������")]
    public ResourceBundle ConstructionCost;

    [Tooltip("������� ���������� ������ �� �����")]
    public List<PlacementRule> PlacementRules;

    [Tooltip("������������ �������� ������")]
    public int MaxHealth;

    [Tooltip("������ ������ � �������")]
    public int Width = 1;

    [Tooltip("������ ������ � �������")]
    public int Height = 1;

    [Header("Production Settings")]
    public ResourceBundle InputResources;
    public ResourceBundle OutputResources;
}