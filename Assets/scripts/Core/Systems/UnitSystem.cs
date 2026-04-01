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
        if (unit && unit.Owner)
        {
            List <Unit> allUnits = _allUnitsDict[unit.Owner];
            if (!allUnits.Contains(unit)) allUnits.Add(unit);
        }
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit && unit.Owner)
        {
            List<Unit> allUnits = _allUnitsDict[unit.Owner];
            if (allUnits.Contains(unit)) allUnits.Remove(unit);
        }
    }

    public void RegisterPlayer(Player player)
    {
        _allUnitsDict.Add(player, new List<Unit>());
    }

    public void UnregisterPlayer(Player player)
    {
        
    }

    private void OnUnitCommandReceived(UnitCommandEvent evt)
    {
        // Логика поиска свободного юнита
        List<Unit> allUnits = _allUnitsDict[evt.Owner];
        Unit idleUnit = allUnits.Find(u => u.CurrentUnitWork == UnitWork.Idle);

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