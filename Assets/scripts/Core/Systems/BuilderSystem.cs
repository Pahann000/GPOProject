using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Управляет процессом размещения зданий на карте.
/// Обрабатывает пользовательский ввод на клиенте, а проверку и списание
/// ресурсов, назначение владельца и спавн выполняет сервер.
/// </summary>
public class BuilderSystem : NetworkBehaviour, IGameSystem
{
    public string SystemName => "Builder System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private ResourceSystem _resourceSystem;

    // Состояние режима стройки (только клиент)
    private bool _isPlacing;
    private BuildingData _selectedBuilding;
    private GameObject _currentPreview;
    private SpriteRenderer _previewRenderer;
    private Camera _mainCamera;

    [SerializeField] private BuildingAtlas _buildingAtlas;

    [Tooltip("Слои препятствий, которые мешают постройке. Пусто = Obstacle + Building.")]
    [SerializeField] private LayerMask obstacleLayers;

    private ContactFilter2D _obstacleFilter;
    private readonly List<Collider2D> _overlapResults = new List<Collider2D>();

    /// <summary>
    /// Система ресурсов резолвится лениво: на клиенте её нет, а на сервере
    /// порядок Start не гарантирует, что она уже зарегистрирована.
    /// </summary>
    private ResourceSystem Economy
    {
        get
        {
            if (_resourceSystem == null && _kernel != null && _kernel.TryGetSystem(out ResourceSystem resourceSystem))
            {
                _resourceSystem = resourceSystem;
            }

            return _resourceSystem;
        }
    }

    /// <summary>Атлас зданий. Если не назначен в инспекторе — ищется в Resources.</summary>
    public BuildingAtlas Atlas
    {
        get
        {
            if (_buildingAtlas == null)
            {
                _buildingAtlas = UnityEngine.Resources.Load<BuildingAtlas>("BuildingAtlas");
            }

            return _buildingAtlas;
        }
    }

    private void Start()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.RegisterSystem(this);
        }
        else
        {
            Debug.LogError($"[{SystemName}] GameKernel не найден!");
        }
    }

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        _resourceSystem = kernel.TryGetSystem(out ResourceSystem resourceSystem) ? resourceSystem : null;

        LayerMask mask = obstacleLayers.value != 0 ? obstacleLayers : LayerMask.GetMask("Obstacle", "Building");
        _obstacleFilter = new ContactFilter2D();
        _obstacleFilter.SetLayerMask(mask);
        _obstacleFilter.useTriggers = false;

        _mainCamera = Camera.main;

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        // Выделенный сервер мышь не читает.
        if (!isClient || !_isPlacing) return;
        if (_selectedBuilding == null || _currentPreview == null)
        {
            CancelBuilding();
            return;
        }

        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        UpdatePreviewPosition();
        UpdatePreviewVisuals();

        if (Input.GetMouseButtonDown(0))
        {
            if (CanPlaceBuildingAt(_selectedBuilding, _currentPreview.transform.position))
            {
                CmdPlaceBuilding(_selectedBuilding.BuildingTypeName, _currentPreview.transform.position);
                CancelBuilding();
            }
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBuilding();
        }
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        CancelBuilding();
    }

    /// <summary>
    /// Активирует режим строительства выбранного здания. Вызывается из UI кнопок.
    /// </summary>
    /// <param name="buildingData">Данные о здании (цена, иконка).</param>
    public void StartPlacement(BuildingData buildingData)
    {
        if (buildingData == null) return;

        CancelBuilding(); // Сбрасываем предыдущую стройку

        _selectedBuilding = buildingData;
        CreatePreview();
        _isPlacing = true;
    }

    /// <summary>
    /// Отменяет текущий режим стройки и уничтожает голограмму-превью.
    /// </summary>
    public void CancelBuilding()
    {
        if (_currentPreview != null)
        {
            Destroy(_currentPreview);
        }

        _isPlacing = false;
        _selectedBuilding = null;
        _currentPreview = null;
        _previewRenderer = null;
    }

    /// <summary>
    /// Создает визуальную "голограмму" здания, которая следует за курсором.
    /// </summary>
    private void CreatePreview()
    {
        _currentPreview = new GameObject("BuildingPreview");
        _previewRenderer = _currentPreview.AddComponent<SpriteRenderer>();

        // Превью показывает здание так, как его увидит фракция игрока:
        // у людей и марсиан разные текстуры зданий.
        Sprite previewSprite = ResolvePreviewSprite();
        if (previewSprite != null)
        {
            _previewRenderer.sprite = previewSprite;
        }

        _previewRenderer.sortingOrder = 100;

        // Коллайдер нужен для физической проверки пересечений с препятствиями
        BoxCollider2D collider = _currentPreview.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
        collider.isTrigger = true;
    }

    /// <summary>
    /// Спрайт для голограммы: фракционный вариант, а если он не задан — иконка из данных.
    /// </summary>
    private Sprite ResolvePreviewSprite()
    {
        FactionType faction = LocalPlayerFaction();
        Sprite factionSprite = FactionAtlas.GetBuildingSprite(faction, _selectedBuilding.BuildingTypeName);

        return factionSprite != null ? factionSprite : _selectedBuilding.Icon;
    }

    private FactionType LocalPlayerFaction()
    {
        var kernel = GameKernel.Instance;
        if (kernel != null && kernel.TryGetSystem(out UISystem uiSystem) && uiSystem.LocalPlayer != null)
        {
            return uiSystem.LocalPlayer.Faction;
        }

        return FactionType.None;
    }

    /// <summary>
    /// Перемещает превью здания за курсором с привязкой к сетке (Snap to Grid).
    /// </summary>
    private void UpdatePreviewPosition()
    {
        // z задаёт плоскость мира: с нулём перспективная камера дала бы точку у самой камеры.
        Vector3 screenPoint = Input.mousePosition;
        screenPoint.z = Mathf.Abs(_mainCamera.transform.position.z);
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(screenPoint);

        // TODO: Добавить поддержку сетки разного размера, если здания будут не 1x1.
        _currentPreview.transform.position = new Vector2(
            Mathf.Round(mousePos.x),
            Mathf.Round(mousePos.y)
        );
    }

    /// <summary>
    /// Окрашивает превью в зеленый или красный цвет в зависимости от возможности постройки.
    /// </summary>
    private void UpdatePreviewVisuals()
    {
        bool canBuild = CanPlaceBuildingAt(_selectedBuilding, _currentPreview.transform.position);
        _previewRenderer.color = canBuild ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
    }

    /// <summary>
    /// Полная проверка места: препятствия -> почва -> дополнительные правила здания.
    /// Вызывается и на клиенте (подсветка превью), и на сервере (защита от читов).
    /// </summary>
    /// <param name="data">Данные здания.</param>
    /// <param name="position">Центр здания.</param>
    private bool CanPlaceBuildingAt(BuildingData data, Vector2 position)
    {
        if (data == null) return false;

        Vector2 checkSize = new Vector2(data.Width, data.Height);

        // 1. Проверка препятствий (пересечение с другими зданиями).
        // Коллайдеры триггеров фильтр не пропускает, поэтому превью само себя не блокирует.
        int overlapCount = Physics2D.OverlapBox(position, checkSize, 0f, _obstacleFilter, _overlapResults);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D overlap = _overlapResults[i];
            if (overlap == null || overlap.gameObject == _currentPreview) continue;

            return false;
        }

        // 2. Проверка взаимодействия с миром (опора под зданием).
        if (_kernel != null && _kernel.TryGetSystem(out WorldSystem world))
        {
            if (!world.IsSurfaceBuildable(position, checkSize)) return false;
        }

        // 3. Дополнительные правила из данных здания (например, «только на поверхности»).
        return data.SatisfiesPlacementRules(position);
    }

    /// <summary>
    /// Префаб здания по типу: единственный источник истины — атлас.
    /// </summary>
    private GameObject GetBuildingPrefab(BuildingTypeNames buildingTypeName)
    {
        BuildingAtlas atlas = Atlas;
        if (atlas == null)
        {
            Debug.LogError($"[{SystemName}] Не назначен BuildingAtlas: постройка невозможна.");
            return null;
        }

        return atlas.GetPrefab(buildingTypeName);
    }

    /// <summary>
    /// Окончательное размещение здания. Выполняется на сервере: проверка места,
    /// списание ресурсов, назначение владельца и фракции, спавн объекта.
    /// </summary>
    [Command(requiresAuthority = false)]
    private void CmdPlaceBuilding(BuildingTypeNames buildingTypeName, Vector2 position, NetworkConnectionToClient sender = null)
    {
        if (!isServer) return;

        // sender == null означает вызов на сервере напрямую (в том числе в режиме Host).
        Player player = sender != null && sender.identity != null
            ? sender.identity.GetComponent<Player>()
            : null;

        if (player == null)
        {
            Debug.LogWarning($"[{SystemName}] Постройка отклонена: не удалось определить игрока.");
            return;
        }

        // Тип здания должен быть открыт исследованием: клиентская панель не даёт
        // нажать кнопку, но команду можно отправить и в обход интерфейса.
        if (!IsBuildingUnlockedFor(player, buildingTypeName))
        {
            Debug.Log($"[{SystemName}] Постройка {buildingTypeName} отклонена: тип не открыт исследованием.");
            return;
        }

        GameObject prefab = GetBuildingPrefab(buildingTypeName);
        if (prefab == null)
        {
            Debug.LogError($"[{SystemName}] Не найден префаб для здания {buildingTypeName}.");
            return;
        }

        // Данные берём у префаба, а не из клиентского поля _selectedBuilding:
        // на сервере оно могло быть другим или пустым.
        Building prefabBuilding = prefab.GetComponent<Building>();
        BuildingData data = prefabBuilding != null ? prefabBuilding.Data : null;
        if (data == null)
        {
            Debug.LogError($"[{SystemName}] У префаба {prefab.name} не задан BuildingData.");
            return;
        }

        // Место проверяется повторно на сервере: клиентская проверка — только
        // подсветка превью, доверять ей нельзя (иначе здание ставится в скалу).
        if (!CanPlaceBuildingAt(data, position))
        {
            Debug.Log($"[{SystemName}] Постройка {data.DisplayName} отклонена: место занято или не подходит.");
            return;
        }

        ResourceSystem resources = Economy;
        if (resources == null)
        {
            Debug.LogError($"[{SystemName}] ResourceSystem недоступен: постройка невозможна.");
            return;
        }

        // Стоимость списывается на сервере: раньше проверка жила только в UI,
        // поэтому здания можно было строить бесплатно.
        if (!resources.TrySpendResources(player, data.ConstructionCost))
        {
            Debug.Log($"[{SystemName}] Постройка {data.DisplayName} отклонена: недостаточно ресурсов.");
            return;
        }

        GameObject buildingObj = Instantiate(prefab, position, Quaternion.identity);

        // Жестко задаем слой, чтобы SelectionSystem мог его найти
        buildingObj.layer = LayerMask.NameToLayer("Building");

        // Коллайдер нужен для выделения мышью. Если он уже есть в префабе,
        // его размер не трогаем — префаб авторитетнее данных о размере.
        BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = buildingObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(data.Width, data.Height);
        }

        Building building = buildingObj.GetComponent<Building>();
        if (building == null)
        {
            Debug.LogError($"[{SystemName}] На префабе {prefab.name} нет компонента Building.");
            Destroy(buildingObj);
            return;
        }

        // Владелец раньше не назначался вообще, из-за чего экономика здания
        // падала на null-ключе, а стоимость не возвращалась.
        building.Owner = player;

        // Фракция нужна клиентам, чтобы нарисовать здание своей текстуры.
        building.SetFactionServer(player.Faction);

        NetworkServer.Spawn(buildingObj);

        _kernel.EventBus.Raise(new BuildingPlacedEvent(data, player));
    }

    public bool IsPlacingBuilding() => _isPlacing;

    /// <summary>
    /// Открыт ли игроку тип здания. Если <see cref="ResearchSystem"/> недоступен
    /// (например, на клиенте или до его регистрации), ограничение не применяется.
    /// </summary>
    private static bool IsBuildingUnlockedFor(Player player, BuildingTypeNames type)
    {
        var kernel = GameKernel.Instance;
        if (kernel == null) return true;
        if (!kernel.TryGetSystem(out ResearchSystem research)) return true;

        return research.IsBuildingUnlocked(player, type);
    }
}
