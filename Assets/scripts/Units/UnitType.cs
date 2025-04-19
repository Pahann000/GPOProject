using UnityEngine;

/// <summary>
/// ��������� ��� ����� � ��� ��������������.
/// </summary>
[CreateAssetMenu(fileName = "UnitType", menuName = "Unit type")]
public class UnitType : ScriptableObject
{
    /// <summary>
    /// �������� ������������.
    /// </summary>
    public float MoveSpeed;
    /// <summary>
    /// �������� ����� ��������� ������.
    /// </summary>
    public float AttackCooldown;
    /// <summary>
    /// ��������� �����.
    /// </summary>
    public int AttackRange;
    /// <summary>
    /// ���� �� ����.
    /// </summary>
    public int DamagePerHit;
    /// <summary>
    /// ������������ ���� �����.
    /// </summary>
    public UnitTypeName UnitTypeName;
    /// <summary>
    /// ������ �����.
    /// </summary>
    public Sprite Sprite;
}
