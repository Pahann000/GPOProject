using UnityEngine;

/// <summary>
/// ����� ������ - ������, ������������� ������������ ��������� �������.
/// ����������� �� �������� ������ <see cref="Building"/>.
/// </summary>
/// <remarks>
/// ��� ���������� ������������� ����������� ������������ ���������,
/// ��� ����������� - ��������� ���.
/// </remarks>
public class LivingModule : Building
{
    /// <summary>
    /// �������� ���������� ������������� ��������� �������.
    /// </summary>
    /// <value>
    /// ������������� ����� �����, ����������� � ������������� ���������.
    /// </value>
    public int PopulationIncrease;

    /// <summary>
    /// ��������� ������������� ������ ������.
    /// </summary>
    /// <remarks>
    /// �������� ������� ����������, ����� ����������� ������������ ��������� �������
    /// �� �������� <see cref="PopulationIncrease"/> ����� <see cref="ColonyManager"/>.
    /// </remarks>
    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        //ColonyManager.Instance.MaxPopulation += PopulationIncrease;
    }

    /// <summary>
    /// ���������� ����� ������.
    /// </summary>
    /// <remarks>
    /// ��������� ������������ ��������� ������� �� �������� <see cref="PopulationIncrease"/>,
    /// ����� �������� ������� ���������� ����������� ������.
    /// </remarks>
    protected override void DestroyBuilding()
    {
        //ColonyManager.Instance.MaxPopulation -= PopulationIncrease;
        base.DestroyBuilding();
    }
}