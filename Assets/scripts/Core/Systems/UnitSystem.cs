using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Главный контроллер всех юнитов в игре.
/// Принимает приказы от игрока (через EventBus) и распределяет их между свободными рабочими.
/// </summary>
public class UnitSystem : IGameSystem
{
    public string SystemName => "Unit System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private Dictionary<Player, List<Unit>> _allUnitsDict = new Dictionary<Player, List<Unit>>();

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        // Подписываемся на команды игрока через шину
        _kernel.EventBus.Subscribe<UnitCommandEvent>(OnUnitCommandReceived);

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime) { }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        _kernel.EventBus.Unsubscribe<UnitCommandEvent>(OnUnitCommandReceived);
        _allUnitsDict.Clear();
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

    private void OnUnitCommandReceived(UnitCommandEvent evt)
    {
        if (evt.Owner == null) return;

        if (!_allUnitsDict.TryGetValue(evt.Owner, out var allUnits) || allUnits == null)
        {
            Debug.LogWarning($"[{SystemName}] Приказ проигнорирован: игрок не зарегистрирован.");
            return;
        }

        // Логика поиска свободного юнита
        Unit idleUnit = allUnits.Find(u => u != null && u.CurrentUnitWork == UnitWork.Idle);

        if (idleUnit != null)
        {
            idleUnit.SetTarget(evt.Target);
            Debug.Log($"[{SystemName}] Юнит {idleUnit.name} отправлен на задание.");
        }
        else
        {
            Debug.LogWarning($"[{SystemName}] Нет свободных юнитов для выполнения приказа!");
        }
    }
}