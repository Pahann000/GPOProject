using UnityEngine;

/// <summary>
/// ����� ������-���������, ����������� ������� ���������� ������.
/// ������������ ��� ���������� ����������� ������������ ���� ��������.
/// </summary>
public class StorageBuilding : Building
{
    /// <summary>
    /// ��� �������, ������� ������ ��� ������.
    /// </summary>
    public ResourceType StoredResource;

    /// <summary>
    /// �������� ���������� ����������� ��������� ��� ���������� �������.
    /// </summary>
    public int StorageIncrease;

    /// <summary>
    /// ��������� ������� ������������� ������-���������.
    /// ����� ���������� ������������� ����������� ��������� ��������� ��� ���������� �������.
    /// </summary>
    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        ResourceManager.Instance.IncreaseStorage(StoredResource, StorageIncrease);
    }

    /// <summary>
    /// ���������� ������-���������.
    /// ����� ������������ ��������� ��������� ��������� ��� ���������� �������.
    /// </summary>
    protected override void DestroyBuilding()
    {
        ResourceManager.Instance.DecreaseStorage(StoredResource, StorageIncrease);
        base.DestroyBuilding();
    }
}