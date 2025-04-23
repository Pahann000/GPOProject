using UnityEngine;

/// <summary>
/// �������������� - ���������������� ������, ������������ �������.
/// ��������� ������� ������ ������������ �� <see cref="ProductionBuilding"/>.
/// </summary>
/// <remarks>
/// ���������� ������������� ���������� ������� ����� �������� ��������� �������.
/// </remarks>
public class PowerPlant : ProductionBuilding
{
    /// <summary>
    /// ���������� �������, ������������ �� ���� ����.
    /// </summary>
    /// <value>
    /// ������������� ����� �����, ������������ ����� �������.
    /// </value>
    public int EnergyOutput;

    /// <summary>
    /// ���������� ����������������� ����� ��� ��������� �������.
    /// </summary>
    /// <remarks>
    /// ���������� ������������� ����� �������� ��������� <see cref="ProductionBuilding.ProductionInterval"/>.
    /// ��������� ��������� ���������� <see cref="EnergyOutput"/> � ����� ��� ��������.
    /// </remarks>
    protected override void ProduceResource()
    {
        // ��������� ������� � ����� ���
        //ResourceManager.Instance.AddResource(ResourceType.Energy, EnergyOutput);
    }
}