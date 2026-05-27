using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AISystem : IGameSystem
{
    public string SystemName => "AI System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private FactionManager _myFaction;
    private FactionType _enemyFaction;
    private FogOfWarSystem _fog;
    private UnitSystem _unitSystem;
    private BuilderSystem _builderSystem;
    private ResourceSystem _resourceSystem;
    private float _decisionTimer = 0f;
    private float _decisionInterval = 1f; // раз в секунду

    public void Initialize(GameKernel kernel, FactionType controlledFaction)
    {
        _kernel = kernel;
        _myFaction = _kernel.GetFactionManager(controlledFaction);
        _enemyFaction = controlledFaction == FactionType.Human ? FactionType.Martian : FactionType.Human;
        _fog = _kernel.GetSystem<FogOfWarSystem>();
        _unitSystem = _kernel.GetSystem<UnitSystem>();
        _builderSystem = _kernel.GetSystem<BuilderSystem>();
        _resourceSystem = _kernel.GetSystem<ResourceSystem>();
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        _decisionTimer += deltaTime;
        if (_decisionTimer >= _decisionInterval)
        {
            _decisionTimer = 0f;
            MakeDecision();
        }
    }

    private void MakeDecision()
    {
        // 1. Найти видимых врагов
        List<Unit> visibleEnemies = FindVisibleEnemies();
        if (visibleEnemies.Count > 0)
        {
            // Атаковать ближайшего
            AttackNearestEnemy(visibleEnemies);
            return;
        }

        // 2. Если мало ресурсов — построить добытчика
        if (NeedMoreResources())
        {
            TryBuildProductionBuilding();
            return;
        }

        // 3. Если есть свободные юниты — отправить на разведку
        if (HasIdleUnits())
        {
            SendScout();
            return;
        }

        // 4. По умолчанию — построить оборону или боевой юнит
        TryBuildDefense();
    }

    private List<Unit> FindVisibleEnemies()
    {
        List<Unit> enemies = new List<Unit>();
        var enemyManager = _kernel.GetFactionManager(_enemyFaction);
        if (enemyManager == null) return enemies;

        foreach (var unit in enemyManager.GetUnits())
        {
            if (unit == null) continue;
            // Проверяем, видит ли ИИ этого юнита
            if (_fog.IsVisible(_myFaction.GetFaction(), unit.X, unit.Y))
                enemies.Add(unit);
        }
        return enemies;
    }

    private void AttackNearestEnemy(List<Unit> enemies)
    {
        Unit closest = null;
        float minDist = float.MaxValue;
        var myUnits = _myFaction.GetUnits();
        foreach (var enemy in enemies)
        {
            foreach (var myUnit in myUnits)
            {
                if (myUnit.CurrentUnitWork != UnitWork.Idle) continue;
                float dist = Vector2.Distance(
                    new Vector2(myUnit.X, myUnit.Y),
                    new Vector2(enemy.X, enemy.Y)
                );
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = enemy;
                }
            }
        }
        if (closest != null)
        {
            // Отдаём команду через EventBus
            var commandEvent = new UnitCommandEvent(closest, new Vector2(closest.X, closest.Y), _myFaction.GetFaction());
            _kernel.EventBus.Raise(commandEvent);
        }
    }

    private bool NeedMoreResources()
    {
        int minerals = _resourceSystem.GetResource(_myFaction.GetFaction(), ResourceType.Minerals);
        int roots = _resourceSystem.GetResource(_myFaction.GetFaction(), ResourceType.Root);
        if (_myFaction.GetFaction() == FactionType.Human)
            return minerals < 200;
        else
            return roots < 200;
    }

    private void TryBuildProductionBuilding()
    {
        // Пример: построить шахту (люди) или комнату корней (марсиане)
        BuildingData buildingToBuild = GetAppropriateProductionBuilding();
        if (buildingToBuild != null && _builderSystem != null)
        {
            // Находим свободного строителя
            var builderUnit = _myFaction.GetUnits().FirstOrDefault(u => u.CurrentUnitWork == UnitWork.Idle && u.unitType.Role == UnitRole.Builder);
            if (builderUnit != null)
            {
                // Нужен метод для начала стройки от AI (или напрямую вызвать StartPlacement с фейковым игроком)
                // Проще создать метод в BuilderSystem для AI: StartPlacementForAI(buildingData, faction)
                _builderSystem.StartPlacementForAI(buildingToBuild, _myFaction.GetFaction());
            }
        }
    }

    private bool HasIdleUnits()
    {
        return _myFaction.GetUnits().Any(u => u.CurrentUnitWork == UnitWork.Idle);
    }

    private void SendScout()
    {
        // Выбираем неисследованную точку карты
        var unexplored = FindUnexploredTile();
        if (unexplored != null)
        {
            var scout = _myFaction.GetUnits().FirstOrDefault(u => u.CurrentUnitWork == UnitWork.Idle);
            if (scout != null)
            {
                var command = new UnitCommandEvent(null, unexplored.Value, _myFaction.GetFaction());
                _kernel.EventBus.Raise(command);
            }
        }
    }

    private Vector2? FindUnexploredTile()
    {
        // Простой поиск: сканируем карту в пределах видимости
        int radius = 20;
        var start = GetRandomBasePosition();
        for (int i = 0; i < 100; i++)
        {
            int x = start.x + Random.Range(-radius, radius);
            int y = start.y + Random.Range(-radius, radius);
            if (x >= 0 && x < _fog._width && y >= 0 && y < _fog._height)
            {
                if (!_fog.IsExplored(_myFaction.GetFaction(), x, y))
                    return new Vector2(x, y);
            }
        }
        return null;
    }

    private Vector2Int GetRandomBasePosition()
    {
        var buildings = _myFaction.GetBuildings();
        if (buildings.Count > 0)
            return new Vector2Int((int)buildings[0].transform.position.x, (int)buildings[0].transform.position.y);
        return Vector2Int.zero;
    }

    private void TryBuildDefense()
    {
        // Построить турель (люди) или оборонительный туннель (марсиане)
        // ...
    }

    private BuildingData GetAppropriateProductionBuilding()
    {
        // Загружаем из ресурсов нужный ScriptableObject
        return Resources.Load<BuildingData>("Buildings/Mine"); // пример
    }

    public void FixedTick(float deltaTime) { }
    public void Shutdown() { }
}