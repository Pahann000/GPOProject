using System.Collections.Generic;
using UnityEngine;

public class FactionManager : IGameSystem
{
    public string SystemName => "Faction Manager";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private FactionType _faction;
    private List<Unit> _units = new List<Unit>();
    private List<Building> _buildings = new List<Building>();

    public void Initialize(GameKernel kernel, FactionType faction)
    {
        _kernel = kernel;
        _faction = faction;
    }

    public void RegisterUnit(Unit unit) { if (!_units.Contains(unit)) _units.Add(unit); }
    public void UnregisterUnit(Unit unit) { _units.Remove(unit); }
    public void RegisterBuilding(Building building) { if (!_buildings.Contains(building)) _buildings.Add(building); }
    public void UnregisterBuilding(Building building) { _buildings.Remove(building); }

    public List<Unit> GetUnits() => _units;
    public List<Building> GetBuildings() => _buildings;
    public FactionType GetFaction() => _faction;

    // Методы для удобного доступа к ресурсам
    public bool HasResources(ResourceBundle cost)
        => _kernel.GetSystem<ResourceSystem>().HasResources(_faction, cost);
    public bool SpendResources(ResourceBundle cost)
        => _kernel.GetSystem<ResourceSystem>().TrySpendResources(_faction, cost);
    public void AddResources(ResourceBundle income)
        => _kernel.GetSystem<ResourceSystem>().AddResources(_faction, income);
}