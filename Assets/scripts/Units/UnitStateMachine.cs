using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Состояния для State Machine юнита.
/// </summary>
public enum UnitState
{
    Idle,
    Moving,
    Attacking,
    Dead
}

/// <summary>
/// Базовое состояние AI юнита.
/// </summary>
public abstract class UnitStateBase
{
    protected Unit _unit;

    public UnitStateBase(Unit unit)
    {
        _unit = unit;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

/// <summary>
/// Состояние простоя.
/// </summary>
public class IdleState : UnitStateBase
{
    public IdleState(Unit unit) : base(unit) { }

    public override void Enter()
    {
        _unit.CurrentUnitWork = UnitWork.Idle;
    }

    public override void Update()
    {
        // Если появилась новая цель, переключаемся
        if (_unit.Target != null)
        {
            _unit.StateMachine.ChangeState(new AttackingState(_unit));
        }
    }

    public override void Exit() { }
}

/// <summary>
/// Состояние движения. Юнит идёт по маршруту, прыгает там, где маршрут требует
/// прыжка (через яму), и пересчитывает путь, если упёрся в препятствие.
/// </summary>
public class MovingState : UnitStateBase
{
    /// <summary>Насколько близко к точке нужно подойти, чтобы считать её достигнутой.</summary>
    private const float ArriveThreshold = 0.5f;

    /// <summary>
    /// Насколько юнит должен приблизиться к текущей точке маршрута, чтобы движение
    /// считалось успешным. Меряем именно приближение к точке, а не пройденный путь:
    /// прыжки на месте и скольжение вдоль стены иначе выглядели бы как прогресс.
    /// </summary>
    private const float ProgressDistance = 0.5f;

    /// <summary>Через сколько секунд без продвижения проверяем обстановку поиском пути.</summary>
    private const float BlockedSeconds = 0.4f;

    /// <summary>Сколько секунд без продвижения означают, что цель недостижима.</summary>
    private const float StuckSeconds = 10f;

    /// <summary>Сколько раз разрешено пересчитать маршрут в рамках одного приказа.</summary>
    private const int MaxReplans = 3;

    private readonly Vector2Int _targetPos;
    private readonly List<GridPathfinder.PathStep> _path;
    private readonly int _replans;
    private int _pathIndex;

    private float _bestDistanceToPoint;
    private float _stuckTimer;
    private bool _blockedChecked;

    public MovingState(Unit unit, Vector2Int targetPos, List<GridPathfinder.PathStep> path, int replans = 0)
        : base(unit)
    {
        _targetPos = targetPos;
        _path = path;
        _replans = replans;
    }

    public override void Enter()
    {
        if (_path == null)
        {
            // Путь не найден: цель недостижима. Переход делаем в Update,
            // чтобы не менять состояние из Enter.
            return;
        }

        _pathIndex = 0;
        ResetProgress();
        _unit.CurrentUnitWork = UnitWork.Moving;
    }

    private void ResetProgress()
    {
        _bestDistanceToPoint = float.MaxValue;
        _stuckTimer = 0f;
        _blockedChecked = false;
    }

    public override void Update()
    {
        if (_path == null)
        {
            CancelOrder($"Путь к цели ({_targetPos.x},{_targetPos.y}) не найден.");
            return;
        }

        if (_pathIndex >= _path.Count)
        {
            // Точка назначения достигнута (пустой путь = уже на месте) — атакуем.
            Debug.Log($"[Unit] Юнит дошёл до ({_targetPos.x},{_targetPos.y}) и приступает к работе.");
            _unit.CurrentUnitWork = UnitWork.Attacking;
            _unit.StateMachine.ChangeState(new AttackingState(_unit));
            return;
        }

        Vector3 position = _unit.transform.position;
        GridPathfinder.PathStep step = _path[_pathIndex];

        // Клетка маршрута — это её центр: юнит стоит в центре клетки, поэтому
        // сравнение с углом клетки давало постоянные 0.5 по высоте и прибытие
        // срабатывало на самой границе допуска.
        Vector3 center = new Vector3(step.X + 0.5f, step.Y + 0.5f, position.z);
        Vector3 toPoint = center - position;
        float distanceToPoint = toPoint.magnitude;

        if (_bestDistanceToPoint - distanceToPoint >= ProgressDistance)
        {
            // Юнит приблизился к точке маршрута — попытка успешна.
            _bestDistanceToPoint = distanceToPoint;
            _stuckTimer = 0f;
            _blockedChecked = false;
        }
        else
        {
            if (distanceToPoint < _bestDistanceToPoint) _bestDistanceToPoint = distanceToPoint;

            _stuckTimer += Time.deltaTime;

            // Препятствие проверяем поиском пути: либо находим обход и идём по нему,
            // либо отменяем приказ. Раньше юнит просто прыгал в стену до таймаута.
            if (!_blockedChecked && _unit.IsGrounded && _stuckTimer >= BlockedSeconds)
            {
                _blockedChecked = true;
                ReplanAroundObstacle();
                return;
            }

            if (_stuckTimer >= StuckSeconds)
            {
                CancelOrder($"Юнит не может дойти до ({_targetPos.x},{_targetPos.y}).");
                return;
            }
        }

        if (step.IsJump)
        {
            UpdateJump(step, position, distanceToPoint);
            return;
        }

        if (distanceToPoint <= ArriveThreshold)
        {
            Advance();
            return;
        }

        // Движение через Rigidbody2D: transform.Translate у динамического тела
        // затирается физикой, поэтому раньше юнит не двигался вообще.
        _unit.SetMoveDirection(toPoint.normalized);

        // Подъём на уступ: прыгаем вверх, сохраняя горизонтальную скорость.
        if (center.y > _unit.transform.position.y + 0.5f && _unit.CanJump)
        {
            _unit.Jump(Vector2.zero);
        }
    }

    /// <summary>
    /// Переход, отмеченный как прыжок: пока юнит в полёте, горизонтальную скорость
    /// не трогаем (иначе прыжок не долетит), а перед прыжком ждём паузу между прыжками.
    /// </summary>
    private void UpdateJump(GridPathfinder.PathStep step, Vector3 position, float distanceToPoint)
    {
        if (distanceToPoint <= ArriveThreshold)
        {
            Advance();
            return;
        }

        // В полёте ничего не делаем: дуга прыжка должна доиграть.
        if (!_unit.IsGrounded) return;

        if (!_unit.CanJump) return;

        float dx = step.X + 0.5f - position.x;
        float direction = Mathf.Abs(dx) < 0.1f ? 0f : Mathf.Sign(dx);

        _unit.Jump(new Vector2(direction, 0f));
    }

    private void Advance()
    {
        _pathIndex++;
        _unit.StopMoving();
        ResetProgress();
    }

    /// <summary>
    /// Проверяет препятствие поиском пути: если появился обход — идём по нему,
    /// иначе приказ отменяется.
    /// </summary>
    private void ReplanAroundObstacle()
    {
        if (_replans >= MaxReplans)
        {
            CancelOrder($"Обход не помог, цель ({_targetPos.x},{_targetPos.y}) недостижима.");
            return;
        }

        int reach = _unit.unitType != null ? Mathf.Max(1, _unit.unitType.AttackRange) : 1;

        List<GridPathfinder.PathStep> fresh = Pathfinding.FindPath(
            new Vector2Int(_unit.X, _unit.Y), _targetPos, reach, _unit.MoveRules);

        if (fresh == null || fresh.Count == 0)
        {
            CancelOrder($"Обхода к ({_targetPos.x},{_targetPos.y}) нет.");
            return;
        }

        Debug.Log($"[Unit] Препятствие на маршруте: путь пересчитан, точек {fresh.Count}.");
        _unit.StateMachine.ChangeState(new MovingState(_unit, _targetPos, fresh, _replans + 1));
    }

    private void CancelOrder(string reason)
    {
        Debug.LogWarning($"[Unit] {reason} Приказ отменён.");
        _unit.Target = null;
        _unit.CurrentUnitWork = UnitWork.Idle;
        _unit.StopMoving();
        _unit.StateMachine.ChangeState(new IdleState(_unit));
    }

    public override void Exit() { }
}

/// <summary>
/// Состояние атаки.
/// </summary>
public class AttackingState : UnitStateBase
{
    private float _attackTimer = 0f;

    public AttackingState(Unit unit) : base(unit) { }

    public override void Enter()
    {
        _unit.CurrentUnitWork = UnitWork.Attacking;
    }

    public override void Update()
    {
        // Цель могла быть уничтожена как игровой объект: обращение к её
        // transform дало бы MissingReferenceException.
        if (_unit.Target is UnityEngine.Object unityTarget && unityTarget == null)
        {
            _unit.Target = null;
        }

        if (_unit.Target == null || _unit.Target.CurrentHealth <= 0)
        {
            _unit.Target = null;
            _unit.StateMachine.ChangeState(new IdleState(_unit));
            return;
        }

        if (_unit.unitType == null) return;

        _attackTimer += Time.deltaTime;
        if (_attackTimer >= _unit.unitType.AttackCooldown)
        {
            _attackTimer = 0f;
            Attack();
        }
    }

    private void Attack()
    {
        UnitType type = _unit.unitType;

        if (Vector2.Distance(_unit.transform.position, new Vector2(_unit.Target.X, _unit.Target.Y)) > type.AttackRange)
        {
            // Пересчитываем путь до клетки В ПРЕДЕЛАХ радиуса добычи, а не до самого
            // блока: путь «на блок» приводил к тому, что юнит упирался в него и стоял.
            List<GridPathfinder.PathStep> path = Pathfinding.FindPath(new Vector2Int(_unit.X, _unit.Y),
                new Vector2Int(_unit.Target.X, _unit.Target.Y), type.AttackRange, _unit.MoveRules);

            if (path == null)
            {
                // Раньше приказ здесь терялся молча: юнит просто оставался стоять.
                Debug.LogWarning($"[Unit] Подойти к цели ({_unit.Target.X},{_unit.Target.Y}) не удалось — приказ отменён.");
                _unit.Target = null;
                _unit.StateMachine.ChangeState(new IdleState(_unit));
                return;
            }

            _unit.StateMachine.ChangeState(new MovingState(_unit, new Vector2Int(_unit.Target.X, _unit.Target.Y), path));
            return;
        }

        _unit.Target.TakeDamage(GetDamage(type), _unit.Owner);

        if (_unit.Target.CurrentHealth <= 0)
        {
            _unit.Target = null;
            _unit.StateMachine.ChangeState(new IdleState(_unit));
        }
    }

    /// <summary>
    /// Итоговый урон атакующего: солдат бьёт вдвое сильнее, остальные — вдвое
    /// слабее (но не меньше 1, иначе урон 1 пропадал из-за целочисленного деления).
    /// Раньше этот множитель считался в получателе урона по типу атакующего.
    /// </summary>
    private static int GetDamage(UnitType type)
    {
        return type.DamagePerHit;
    }

    public override void Exit() { }
}

/// <summary>
/// Состояние смерти. Терминальное: выйти из него нельзя.
/// </summary>
public class DeadState : UnitStateBase
{
    public DeadState(Unit unit) : base(unit) { }

    public override void Enter()
    {
        _unit.CurrentUnitWork = UnitWork.Dead;
    }

    public override void Update()
    {
        // Ждем пока игрок удалит труп
    }

    public override void Exit() { }
}

/// <summary>
/// State Machine для AI юнита.
/// </summary>
public class UnitStateMachine
{
    private Unit _unit;
    private UnitStateBase _currentState;

    /// <summary>
    /// Текущее состояние. Смена — только через <see cref="ChangeState"/>,
    /// иначе предыдущее состояние не получает Exit().
    /// </summary>
    public UnitStateBase CurrentState => _currentState;

    /// <summary>Юнит мёртв: состояние терминально, новые переходы игнорируются.</summary>
    public bool IsDead { get; private set; }

    public UnitStateMachine(Unit unit)
    {
        _unit = unit;
        ChangeState(new IdleState(unit));
    }

    public void SetTarget(IDamagable target)
    {
        if (IsDead || target == null) return;

        _unit.Target = target;

        // Юниту не нужно вставать на саму цель: достаточно дойти до клетки
        // в пределах радиуса добычи, откуда он её и обрабатывает.
        int reach = _unit.unitType != null ? Mathf.Max(1, _unit.unitType.AttackRange) : 1;

        // Если юнит уже достаёт до цели — никуда не идём, сразу работаем.
        if (Vector2.Distance(_unit.transform.position, new Vector2(target.X, target.Y)) <= reach)
        {
            ChangeState(new AttackingState(_unit));
            return;
        }

        List<GridPathfinder.PathStep> path = Pathfinding.FindPath(
            new Vector2Int(_unit.X, _unit.Y),
            new Vector2Int(target.X, target.Y),
            reach,
            _unit.MoveRules);

        if (path == null)
        {
            // Без этого сообщения «юнит получил приказ и ничего не делает»
            // невозможно объяснить: цель внутри породы или отрезана от юнита.
            Debug.LogWarning($"[Unit] Путь к цели ({target.X},{target.Y}) не найден — цель недостижима.");
            _unit.Target = null;
            return;
        }

        ChangeState(new MovingState(_unit, new Vector2Int(target.X, target.Y), path));
    }

    public void Update()
    {
        if (IsDead) return;

        _currentState?.Update();
    }

    public void Kill()
    {
        if (IsDead) return;

        IsDead = true;
        _unit.Target = null;
        ChangeState(new DeadState(_unit));
    }

    /// <summary>
    /// Единственная точка смены состояния: всегда вызывает Exit предыдущего
    /// и не позволяет выйти из Dead.
    /// </summary>
    public void ChangeState(UnitStateBase next)
    {
        if (next == null) return;
        if (IsDead && !(next is DeadState)) return;

        _currentState?.Exit();

        // Любая смена состояния останавливает горизонтальное движение:
        // иначе юнит продолжал бы скользить по инерции.
        _unit.StopMoving();

        _currentState = next;
        _currentState.Enter();
    }
}
