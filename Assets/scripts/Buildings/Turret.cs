using UnityEngine;

/// <summary>
/// Класс турели, представляющий оборонительное сооружение, способное атаковать врагов.
/// Наследуется от базового класса <see cref="Building"/>.
/// </summary>
public class Turret : Building
{
    /// <summary>
    /// Дальность действия турели (радиус обнаружения целей).
    /// </summary>
    public int DefenseRange;

    /// <summary>
    /// Урон, наносимый турелью за одну атаку.
    /// </summary>
    public int Damage;

    /// <summary>
    /// Время между атаками (в секундах).
    /// </summary>
    public float AttackCooldown;

    private float _lastAttackTime;
    private GameObject _currentTarget;

    /// <summary>
    /// Обновляет состояние турели каждый кадр.
    /// Если турель функционирует (<see cref="Building.State"/> == <see cref="BuildingState.Operational"/>),
    /// ищет цели и атакует их при соблюдении условий.
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
    /// Поиск цели в радиусе действия турели (<see cref="DefenseRange"/>).
    /// </summary>
    /// <remarks>
    /// При обнаружении врага устанавливает его в качестве текущей цели (<see cref="_currentTarget"/>).
    /// Если врагов нет, сбрасывает цель в <c>null</c>.
    /// </remarks>
    private void FindTarget()
    {
        // Логика поиска целей
    }

    /// <summary>
    /// Производит атаку текущей цели (<see cref="_currentTarget"/>).
    /// </summary>
    /// <remarks>
    /// Наносит урон (<see cref="Damage"/>) цели, если она существует.
    /// </remarks>
    private void Attack()
    {
        // Логика атаки
    }
}