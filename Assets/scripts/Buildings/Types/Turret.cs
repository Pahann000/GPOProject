using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Оборонительное сооружение: ищет врагов в радиусе действия и атакует их.
/// Работает только на сервере — как и вся боевая логика.
/// </summary>
/// <remarks>
/// Раньше <c>FindTarget()</c> и <c>Attack()</c> были пустыми, а <c>_currentTarget</c>
/// не присваивался нигде, поэтому атака была недостижимым кодом (и компилятор
/// предупреждал об этом: CS0649).
/// </remarks>
public class Turret : Building
{
    [Header("Турель")]
    [Tooltip("Радиус обнаружения целей.")]
    [SerializeField] private float defenseRange = 6f;

    [Tooltip("Урон за одну атаку.")]
    [SerializeField] private int damage = 5;

    [Tooltip("Время между атаками в секундах.")]
    [SerializeField] private float attackCooldown = 1f;

    [Tooltip("Слои, на которых ищутся цели. Пусто = Default.")]
    [SerializeField] private LayerMask targetLayers;

    /// <summary>
    /// Юниты и препятствия лежат на Default — это маска по умолчанию.
    /// Вычисляется лениво: LayerMask.GetMask нельзя вызывать из статического
    /// инициализатора MonoBehaviour (Unity бросает UnityException).
    /// </summary>
    private static int _defaultTargetMask = -1;

    private static int DefaultTargetMask
    {
        get
        {
            if (_defaultTargetMask < 0)
            {
                _defaultTargetMask = LayerMask.GetMask("Default");
            }

            return _defaultTargetMask;
        }
    }

    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();
    private ContactFilter2D _targetFilter;
    private float _lastAttackTime;
    private Unit _currentTarget;

    protected override void Start()
    {
        base.Start();

        _targetFilter = new ContactFilter2D();
        _targetFilter.SetLayerMask(targetLayers.value != 0 ? targetLayers : DefaultTargetMask);
        _targetFilter.useTriggers = false;
    }

    /// <summary>
    /// Обновляет состояние турели каждый кадр.
    /// Если турель функционирует (<see cref="Building.State"/> == <see cref="BuildingState.Operational"/>),
    /// ищет цели и атакует их при соблюдении условий.
    /// </summary>
    public override void Update()
    {
        if (!isServer) return;

        // Производство базового класса (у турели его нет, но поведение базового типа не теряем).
        base.Update();

        if (State != BuildingState.Operational) return;

        if (!HasValidTarget())
        {
            _currentTarget = FindTarget();
        }

        if (_currentTarget == null) return;
        if (Time.time - _lastAttackTime < attackCooldown) return;

        Attack(_currentTarget);
        _lastAttackTime = Time.time;
    }

    private bool HasValidTarget()
    {
        if (_currentTarget == null) return false;
        if (_currentTarget.CurrentHealth <= 0) return false;

        return Vector2.Distance(transform.position, _currentTarget.transform.position) <= defenseRange;
    }

    /// <summary>
    /// Поиск ближайшего врага в радиусе действия турели (<see cref="defenseRange"/>).
    /// </summary>
    /// <remarks>
    /// Свои юниты целью не становятся. Буфер коллайдеров переиспользуется,
    /// чтобы не аллоцировать массив каждый кадр.
    /// </remarks>
    private Unit FindTarget()
    {
        int count = Physics2D.OverlapCircle(transform.position, defenseRange, _targetFilter, _overlapResults);

        Unit best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Unit candidate = _overlapResults[i].GetComponentInParent<Unit>();
            if (candidate == null) continue;
            if (candidate.CurrentHealth <= 0) continue;
            if (candidate.Owner != null && candidate.Owner == Owner) continue;

            float distance = Vector2.Distance(transform.position, candidate.transform.position);
            if (distance >= bestDistance) continue;

            best = candidate;
            bestDistance = distance;
        }

        return best;
    }

    /// <summary>
    /// Производит атаку текущей цели.
    /// </summary>
    private void Attack(Unit target)
    {
        target.TakeDamage(damage, Owner);
    }
}
