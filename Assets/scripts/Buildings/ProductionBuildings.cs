using UnityEngine;

/// <summary>
/// ����������� ����� ����������������� ������, ���������� ������� � �������� ����������.
/// ����������� �� �������� ������ <see cref="Building"/>.
/// </summary>
/// <remarks>
/// ��������� ������� ������ ������������ ��������, �������� ���������� ����������
/// ������������ �� ���������� �������� �������.
/// </remarks>
public abstract class ProductionBuilding : Building
{
    [Header("Production Settings")]
    /// <summary>
    /// ��� �������, ������������� �������.
    /// </summary>
    public ResourceType ProducedResource;

    /// <summary>
    /// ���������� ��������, ������������ �� ���� ����.
    /// </summary>
    public int ProductionAmount;

    /// <summary>
    /// �������� ����� ������� ������������ (� ��������).
    /// </summary>
    public float ProductionInterval;

    private float _lastProductionTime;

    /// <summary>
    /// ��������� ��������� ������ ������ ����.
    /// </summary>
    /// <remarks>
    /// ���� ������ ��������� � ������� ���������, ��������� �������������
    /// ���������� ����� ���� ������������ �� ������ ��������� ���������.
    /// �������� ������� ���������� Update �� <see cref="Building"/>.
    /// </remarks>
    private void Update()
    {
        base.Update();

        if (State == BuildingState.Operational &&
            Time.time - _lastProductionTime > ProductionInterval)
        {
            ProduceResource();
            _lastProductionTime = Time.time;
        }
    }

    /// <summary>
    /// ����������� ����� ������������ ��������.
    /// </summary>
    /// <remarks>
    /// ������ ���� ���������� � �������� �������. �������� ������
    /// �������� � ������������� ������������ ��������.
    /// ���������� ������������� �� ��������� ����������������� ���������.
    /// </remarks>
    protected abstract void ProduceResource();
}