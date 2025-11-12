using UnityEngine;

/// <summary>
/// Описывает тип юнита и его характеристики.
/// </summary>
[CreateAssetMenu(fileName = "UnitType", menuName = "Unit type")]
public class UnitType : ScriptableObject
{
    public int Health;
    /// <summary>
    /// Скорость передвижения.
    /// </summary>
    public float MoveSpeed;
    /// <summary>
    /// Задержка перед следующей атакой.
    /// </summary>
    public float AttackCooldown;
    /// <summary>
    /// Дистанция атаки.
    /// </summary>
    public int AttackRange;
    /// <summary>
    /// Урон от атак.
    /// </summary>
    public int DamagePerHit;
    /// <summary>
    /// Наименование типа юнита.
    /// </summary>
    public UnitTypeName UnitTypeName;
    /// <summary>
    /// Спрайт юнита.
    /// </summary>
    public Sprite Sprite;
}
