using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Главный контроллер всех юнитов в игре.
/// Принимает задачи от игрока (через EventBus), держит их в очереди и раздаёт
/// ближайшим подходящим свободным юнитам.
/// </summary>
/// <remarks>
/// Распределение работы перенесено из ветки features/9_unit_work_spread_system
/// (коммит 2048b26). Раньше приказ уходил первому свободному юниту в списке,
/// поэтому все юниты бежали к одной цели, а дальние задачи оставались без
/// исполнителя; кроме того приказ терялся, если все юниты были заняты,
/// а очередь задач вообще отсутствовала.
/// </remarks>
public class UnitSystem : IGameSystem
{
    public string SystemName => "Unit System";
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Кандидат на выполнение задачи: тип, занятость и позиция.
    /// Отдельная структура нужна, чтобы выбор исполнителя был чистой логикой
    /// и покрывался тестами без сцены.
    /// </summary>
    public readonly struct UnitCandidate
    {
        public readonly UnitTypeName Type;
        public readonly UnitWork Work;
        public readonly Vector2 Position;

        public UnitCandidate(UnitTypeName type, UnitWork work, Vector2 position)
        {
            Type = type;
            Work = work;
            Position = position;
        }
    }

    private GameKernel _kernel;
    private Dictionary<Player, List<Unit>> _allUnitsDict = new Dictionary<Player, List<Unit>>();

    /// <summary>Задачи, ожидающие свободного подходящего юнита.</summary>
    private readonly List<Task> _pendingTasks = new List<Task>();

    /// <summary>Задачи, уже отданные юнитам.</summary>
    private readonly List<ActiveTask> _activeTasks = new List<ActiveTask>();

    /// <summary>Переиспользуемый буфер кандидатов: распределение идёт каждый кадр.</summary>
    private readonly List<UnitCandidate> _candidates = new List<UnitCandidate>(16);

    public int PendingTaskCount => _pendingTasks.Count;
    public int ActiveTaskCount => _activeTasks.Count;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        // Подписываемся на команды игрока через шину
        _kernel.EventBus.Subscribe<UnitCommandEvent>(OnUnitCommandReceived);

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        // Сначала освобождаем завершённые задачи, потом раздаём освободившимися юнитами новые.
        PruneActiveTasks();
        DistributeTasks();
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        _kernel.EventBus.Unsubscribe<UnitCommandEvent>(OnUnitCommandReceived);
        _allUnitsDict.Clear();
        _pendingTasks.Clear();
        _activeTasks.Clear();
    }

    public void RegisterUnit(Unit unit)
    {
        if (unit == null || unit.Owner == null) return;

        var allUnits = GetOrCreateUnitList(unit.Owner);
        if (!allUnits.Contains(unit)) allUnits.Add(unit);
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit == null || unit.Owner == null) return;

        if (_allUnitsDict.TryGetValue(unit.Owner, out var allUnits) && allUnits != null)
        {
            allUnits.Remove(unit);
        }
    }

    public void RegisterPlayer(Player player)
    {
        if (player == null) return;

        GetOrCreateUnitList(player);
    }

    public void UnregisterPlayer(Player player)
    {
        if (player == null) return;

        _allUnitsDict.Remove(player);
    }

    /// <summary>
    /// Ставит задачу в очередь. Исполнителя подберёт <see cref="DistributeTasks"/>:
    /// ближайшего свободного юнита нужного типа.
    /// </summary>
    public void AddTask(Player owner, IDamagable target, UnitTypeName requiredUnitType)
    {
        if (owner == null || target == null) return;

        _pendingTasks.Add(new Task(owner, target, requiredUnitType));
    }

    /// <summary>
    /// Выбирает ближайшего подходящего юнита: тип совпадает и юнит свободен.
    /// Чистая логика без сцены — покрыта тестами.
    /// </summary>
    /// <returns>Индекс кандидата или -1, если подходящих нет.</returns>
    public static int SelectNearestFreeUnit(IReadOnlyList<UnitCandidate> candidates, UnitTypeName requiredType, Vector2 targetPosition)
    {
        if (candidates == null) return -1;

        int bestIndex = -1;
        float bestSqrDistance = float.MaxValue;

        for (int i = 0; i < candidates.Count; i++)
        {
            UnitCandidate candidate = candidates[i];

            if (candidate.Work != UnitWork.Idle) continue;
            if (candidate.Type != requiredType) continue;

            float sqrDistance = (candidate.Position - targetPosition).sqrMagnitude;
            if (sqrDistance >= bestSqrDistance) continue;

            bestSqrDistance = sqrDistance;
            bestIndex = i;
        }

        return bestIndex;
    }

    /// <summary>
    /// Возвращает список юнитов игрока, создавая его при необходимости.
    /// Порядок «регистрация игрока → спавн юнита» не гарантирован,
    /// поэтому прямой индексатор бросал KeyNotFoundException.
    /// </summary>
    private List<Unit> GetOrCreateUnitList(Player owner)
    {
        if (_allUnitsDict.TryGetValue(owner, out var allUnits) && allUnits != null)
        {
            return allUnits;
        }

        allUnits = new List<Unit>();
        _allUnitsDict[owner] = allUnits;
        return allUnits;
    }

    /// <summary>Раздаёт задачи: каждая уходит ближайшему свободному юниту своего владельца.</summary>
    private void DistributeTasks()
    {
        for (int i = _pendingTasks.Count - 1; i >= 0; i--)
        {
            Task task = _pendingTasks[i];

            if (task == null || !task.IsTargetValid())
            {
                _pendingTasks.RemoveAt(i);
                continue;
            }

            if (!_allUnitsDict.TryGetValue(task.Owner, out var ownerUnits) || ownerUnits == null) continue;

            CollectCandidates(ownerUnits);

            int index = SelectNearestFreeUnit(_candidates, task.RequiredUnitType, task.TargetPosition);
            if (index < 0) continue;   // подходящих свободных нет — задача ждёт в очереди

            Unit unit = FindCandidateUnit(ownerUnits, index);
            if (unit == null) continue;

            unit.SetTarget(task.Target);

            _activeTasks.Add(new ActiveTask { Unit = unit, Task = task });
            _pendingTasks.RemoveAt(i);

            Debug.Log($"[{SystemName}] Задача {task.TargetLabel} отдана ближайшему юниту {unit.name}.");
        }
    }

    /// <summary>Собирает кандидатов в том же порядке, в котором идут живые юниты владельца.</summary>
    private void CollectCandidates(List<Unit> ownerUnits)
    {
        _candidates.Clear();

        for (int i = 0; i < ownerUnits.Count; i++)
        {
            Unit unit = ownerUnits[i];
            if (unit == null) continue;

            _candidates.Add(new UnitCandidate(unit.TypeName, unit.CurrentUnitWork, unit.transform.position));
        }
    }

    /// <summary>Ищет юнита по индексу среди живых: индексы кандидатов и списка совпадают.</summary>
    private static Unit FindCandidateUnit(List<Unit> ownerUnits, int candidateIndex)
    {
        int seen = -1;

        for (int i = 0; i < ownerUnits.Count; i++)
        {
            Unit unit = ownerUnits[i];
            if (unit == null) continue;

            seen++;
            if (seen == candidateIndex) return unit;
        }

        return null;
    }

    /// <summary>
    /// Убирает задачи, которые больше не актуальны: цель уничтожена или юнит уже освободился.
    /// Без этого освободившийся юнит не получил бы следующую задачу.
    /// </summary>
    private void PruneActiveTasks()
    {
        for (int i = _activeTasks.Count - 1; i >= 0; i--)
        {
            ActiveTask active = _activeTasks[i];

            bool unitGone = active.Unit == null;
            bool targetGone = active.Task == null || !active.Task.IsTargetValid();
            bool unitIsFree = !unitGone && active.Unit.CurrentUnitWork == UnitWork.Idle;

            if (unitGone || targetGone || unitIsFree)
            {
                _activeTasks.RemoveAt(i);
            }
        }
    }

    private void OnUnitCommandReceived(UnitCommandEvent evt)
    {
        if (evt.Owner == null || evt.Target == null) return;

        if (!_allUnitsDict.TryGetValue(evt.Owner, out var allUnits) || allUnits == null)
        {
            Debug.LogWarning($"[{SystemName}] Приказ проигнорирован: игрок не зарегистрирован.");
            return;
        }

        // Кому по силам: блок добывает шахтёр, чужого юнита атакует солдат.
        UnitTypeName requiredType = evt.Target is Block ? UnitTypeName.Miner : UnitTypeName.Soldier;

        AddTask(evt.Owner, evt.Target, requiredType);

        Debug.Log($"[{SystemName}] Задача для {requiredType} на цель ({evt.Target.X},{evt.Target.Y}) поставлена в очередь.");
    }
}
