using UnityEngine;

/// <summary>
/// Базовый класс для оборонительных сооружений.
/// </summary>
public abstract class DefenseBuilding : Building
{
    protected DefenseBuildingData DefenseData => _data as DefenseBuildingData;
    protected float _lastAttackTime;

    protected override void Start()
    {
        base.Start();
        if (DefenseData == null)
        {
            Debug.LogError($"DefenseBuilding {name} имеет неверный тип данных!");
            enabled = false;
        }
    }

    public override void Update()
    {
        base.Update();
        if (State != BuildingState.Operational || DefenseData == null) return;

        // Поиск цели и атака с учётом кулдауна
        if (Time.time - _lastAttackTime >= DefenseData.Cooldown)
        {
            GameObject target = FindTarget();
            if (target != null)
            {
                Attack(target);
                _lastAttackTime = Time.time;
            }
        }
    }

    /// <summary>
    /// Поиск цели в радиусе.
    /// </summary>
    protected abstract GameObject FindTarget();

    /// <summary>
    /// Атака цели.
    /// </summary>
    protected abstract void Attack(GameObject target);
}