using UnityEngine;
using Mirror;

/// <summary>
/// Класс, обеспечивающий взаимодействие игрока с игровым миром.
/// </summary>
public class Player : NetworkBehaviour, IChunkObserver
{
    private bool _isRegistered = false;

    private void OnDestroy()
    {
        if (_isRegistered && GameKernel.Instance != null)
        {
            GameKernel.Instance.GetSystem<WorldSystem>()?.WorldChunks?.UnregisterObserver(this);
        }
    }

    /// <summary>
    /// Локальное хранилище ресурсов для работы UI.
    /// </summary>
    public ObservableDictionary<ResourceType, int> Resources { get; } = new ObservableDictionary<ResourceType, int>();

    /// <summary>
    /// Положение игрока по X.
    /// </summary>
    public int X => (int)this.transform.position.x;

    /// <summary>
    /// Положение игрока по X.
    /// </summary>
    public int Y => (int)this.transform.position.y;

    /// <inheritdoc/>
    void Update()
    {
        if (!_isRegistered && GameKernel.Instance != null)
        {
            var worldSys = GameKernel.Instance.GetSystem<WorldSystem>();
            if (worldSys != null && worldSys.WorldChunks != null)
            {
                worldSys.WorldChunks.RegisterObserver(this);
                if (isLocalPlayer)
                {
                    worldSys.CmdSpawnUnit(this);
                }
                _isRegistered = true;
                Debug.Log("[Player] Успешно зарегистрирован в системе чанков.");
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
            OnPlayerClicked(new Vector2(mousePos.origin.x, mousePos.origin.y));
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        RegisterClient();
    }

    [Command]
    public void RegisterClient()
    {
        if (isServer)
        {
            GameKernel.Instance.GetSystem<UnitSystem>().RegisterPlayer(this);
            GameKernel.Instance.GetSystem<ResourceSystem>().RegisterPlayer(this);
        }
    }

    [Command]
    private void OnPlayerClicked(Vector2 position)
    {
        if (isServer)
        {
            SelectTarget(position);
        }
    }

    //TODO: сделать нормальное выделение, и вообще работу игрока с блоками
    /// <summary>
    /// выделяет блок(заглушка заставляющая юнита уничтожить блок).
    /// </summary>
    /// <param name="position">Позиция выделяемого блока.</param>
    [Server]
    private void SelectTarget(Vector2 position)
    {
        IDamagable target = null;
        Block selectedBlock = GameKernel.Instance.GetSystem<WorldSystem>().WorldMap.GetBlockObj(((int)position.x), ((int)position.y));

        if (selectedBlock.tileData.type != BlockType.Air)
        {
            target = selectedBlock;
        }
        else
        {
            Vector3 vector3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var targetCollider = Physics2D.OverlapPoint(new Vector2(vector3.x, vector3.y));
            if (targetCollider)
            {
                Unit unit = targetCollider.gameObject.GetComponent<Unit>();
                if (unit != null && !(unit.Owner == this))
                {
                    target = unit;
                }
                else return;
            }
        }

        if (target != null && GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Raise(new UnitCommandEvent(target, position, this));
        }
    }

    /// <summary>
    /// Сетевая команда от Клиента к Серверу на постройку здания (Тестовая версия).
    /// </summary>
    [Command]
    public void CmdRequestPlaceBuilding(string buildingDataName, Vector2 position)
    {
        if (!isServer) return;

        Debug.Log($"[Server] Запрос на постройку '{buildingDataName}' от Игрока {netId} на позиции {position}");

        // 1. Ищем данные здания
        BaseBuildingData buildingData = UnityEngine.Resources.Load<BaseBuildingData>($"BuildingsData/{buildingDataName}");
        if (buildingData == null)
        {
            BaseBuildingData[] allBuildings = UnityEngine.Resources.LoadAll<BaseBuildingData>("");
            foreach (var b in allBuildings)
            {
                if (b.name == buildingDataName || b.DisplayName == buildingDataName)
                {
                    buildingData = b;
                    break;
                }
            }
        }

        if (buildingData == null)
        {
            Debug.LogError($"[Server] ❌ ОШИБКА 1: Не найден ассет здания '{buildingDataName}' в папке Resources!");
            return;
        }

        var resourceSys = GameKernel.Instance.GetSystem<ResourceSystem>();
        var worldSys = GameKernel.Instance.GetSystem<WorldSystem>();

        // 2. ВРЕМЕННО: На сервере тоже отключаем проверку поверхности и ресурсов для тестового режима!
        /*
        if (resourceSys == null || !resourceSys.HasResources(this, buildingData.ConstructionCost))
        {
            Debug.LogWarning($"[Server] ❌ ОШИБКА 2: У игрока {netId} недостаточно ресурсов для постройки {buildingData.DisplayName}");
            return;
        }

        if (worldSys != null && !worldSys.IsSurfaceBuildable(position, new Vector2(buildingData.Width, buildingData.Height)))
        {
            Debug.LogWarning($"[Server] ❌ ОШИБКА 3: Позиция {position} непригодна для постройки!");
            return;
        }
        */

        // 3. Создаем здание на сервере
        if (buildingData.Prefab == null)
        {
            Debug.LogError($"[Server] ❌ ОШИБКА 4: В ассете {buildingData.DisplayName} не назначен Prefab!");
            return;
        }

        // Попытка списать ресурсы (для логов), но спавним в любом случае
        if (resourceSys != null)
        {
            resourceSys.TrySpendResources(this, buildingData.ConstructionCost);
        }

        GameObject buildingObj = Instantiate(buildingData.Prefab, position, Quaternion.identity);
        buildingObj.layer = LayerMask.NameToLayer("Building");

        Building building = buildingObj.GetComponent<Building>();
        if (building != null)
        {
            buildingData.Owner = this;
            building.Initialize(buildingData);
            building.NotifyBuilt();
        }

        BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = buildingObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(buildingData.Width, buildingData.Height);
        }

        // 4. ВАЖНО: Спавним объект по сети для ВСЕХ клиентов!
        NetworkServer.Spawn(buildingObj, connectionToClient);

        Debug.Log($"<color=green>[SERVER] ✅ УСПЕХ:</color> Здание {buildingData.DisplayName} успешно заспавнено по сети для Игрока {netId}!");
    }
}
