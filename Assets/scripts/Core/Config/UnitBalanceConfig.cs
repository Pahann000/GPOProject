using UnityEngine;

/// <summary>
/// Конфигурация баланса юнитов (ScriptableObject).
/// </summary>
[CreateAssetMenu(fileName = "UnitBalanceConfig", menuName = "Mars Colony Builder/UnitBalanceConfig")]
public class UnitBalanceConfig : ScriptableObject
{
    [Header("Типы юнитов")]
    public UnitTypeConfig[] UnitTypes;

    [System.Serializable]
    public class UnitTypeConfig
    {
        [Tooltip("Имя типа юнита")]
        public string UnitTypeName;

        [Tooltip("Урон за удар")]
        public int DamagePerHit;

        [Tooltip("Дальность атаки")]
        public float AttackRange;

        [Tooltip("Таймаут атаки в секундах")]
        public float AttackCooldown;

        [Tooltip("Здоровье юнита")]
        public int Health;

        [Tooltip("Скорость движения")]
        public float Speed;

        [Tooltip("Входные ресурсы для производства")]
        public ResourcePair[] ProductionCost;

        [Tooltip("Выходные ресурсы (если юнит производит)")]
        public ResourcePair[] ProductionOutput;

        [Tooltip("Время производства в секундах")]
        public float ProductionTime;
    }
}
