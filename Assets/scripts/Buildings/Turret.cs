using UnityEngine;

/// <summary>
/// ����� ������, �������������� �������������� ����������, ��������� ��������� ������.
/// ����������� �� �������� ������ <see cref="Building"/>.
/// </summary>
public class Turret : Building
{
    /// <summary>
    /// ��������� �������� ������ (������ ����������� �����).
    /// </summary>
    public int DefenseRange;

    /// <summary>
    /// ����, ��������� ������� �� ���� �����.
    /// </summary>
    public int Damage;

    /// <summary>
    /// ����� ����� ������� (� ��������).
    /// </summary>
    public float AttackCooldown;

    private float _lastAttackTime;
    private GameObject _currentTarget;

    /// <summary>
    /// ��������� ��������� ������ ������ ����.
    /// ���� ������ ������������� (<see cref="Building.State"/> == <see cref="BuildingState.Operational"/>),
    /// ���� ���� � ������� �� ��� ���������� �������.
    /// </summary>
    private void Update()
    {
        if (State != BuildingState.Operational) return;

        FindTarget();

        if (_currentTarget != null && Time.time - _lastAttackTime > AttackCooldown)
        {
            Attack();
            _lastAttackTime = Time.time;
        }
    }

    /// <summary>
    /// ����� ���� � ������� �������� ������ (<see cref="DefenseRange"/>).
    /// </summary>
    /// <remarks>
    /// ��� ����������� ����� ������������� ��� � �������� ������� ���� (<see cref="_currentTarget"/>).
    /// ���� ������ ���, ���������� ���� � <c>null</c>.
    /// </remarks>
    private void FindTarget()
    {
        // ������ ������ �����
    }

    /// <summary>
    /// ���������� ����� ������� ���� (<see cref="_currentTarget"/>).
    /// </summary>
    /// <remarks>
    /// ������� ���� (<see cref="Damage"/>) ����, ���� ��� ����������.
    /// </remarks>
    private void Attack()
    {
        // ������ �����
    }
}