using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ������ ������, ����������� ��� ScriptableObject
/// </summary>
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
    public ResourceCost ConstructionCost;

    [Tooltip("������� ���������� ������ �� �����")]
    public List<PlacementRule> PlacementRules;
}

/// <summary>
/// ���������, ����������� ��������� ��������� � ��������
/// </summary>
[System.Serializable]
public struct ResourceCost
{
    [Tooltip("���������� �������")]
    public int Metal;

    [Tooltip("���������� ���������")]
    public int Minerals;

    [Tooltip("���������� ����")]
    public int Water;
}

