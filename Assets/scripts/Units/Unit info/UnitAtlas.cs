using UnityEngine;

/// <summary>
/// ��������� �����, ���������� ���� ������.
/// </summary>
[CreateAssetMenu(fileName = "UnitAtlas", menuName = "Unit atlas")]
public class UnitAtlas : ScriptableObject
{
    /// <summary>
    /// �����.
    /// </summary>
    public UnitType Miner;
    /// <summary>
    /// �������.
    /// </summary>
    public UnitType Worker;
    /// <summary>
    /// ������.
    /// </summary>
    public UnitType Soldier;
}
