using UnityEngine;

/// <summary>
/// Описывает атлас, содержащий типы юнитов.
/// </summary>
[CreateAssetMenu(fileName = "UnitAtlas", menuName = "Unit atlas")]
public class UnitAtlas : ScriptableObject
{
    /// <summary>
    /// Шахтёр.
    /// </summary>
    public UnitType Miner;
    /// <summary>
    /// Рабочий.
    /// </summary>
    public UnitType Worker;
    /// <summary>
    /// солдат.
    /// </summary>
    public UnitType Soldier;
}
