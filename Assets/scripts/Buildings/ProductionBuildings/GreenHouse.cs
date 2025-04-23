using UnityEngine;

/// <summary>
/// ������� - ���������������� ������ ��� ����������� ��� � ������������ ����.
/// ��������� ������� ������ ������������ �� <see cref="ProductionBuilding"/>.
/// </summary>
/// <remarks>
/// ��� ��������� ������������ ������� ������� ���� � ��������� ����������.
/// ��� ���������� ���� ���������������� ���� ������������.
/// </remarks>
public class Greenhouse : ProductionBuilding
{
    /// <summary>
    /// ���������� ����, ������������ �� ���� ���������������� ����.
    /// </summary>
    /// <value>
    /// ������������� ����� �����, ������������ ������ ����.
    /// </value>
    public int WaterConsumptionPerCycle;

    /// <summary>
    /// ���������� ������� �������, ��������� ����.
    /// </summary>
    protected override void ProduceResource()
    {
        //if (ResourceManager.Instance.TryUseResource(ResourceType.Water, WaterConsumptionPerCycle))
        //{
        //    ResourceManager.Instance.AddResource(ResourceType.Food, ProductionAmount);
        //}
    }
}