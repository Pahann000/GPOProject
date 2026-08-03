using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Управляет процессом размещения зданий на карте.
/// Выполняет полную проверку занимаемой площади, фундамента, ресурсов и коллизий.
/// </summary>
public class BuilderSystem : IGameSystem
{
    public string SystemName => "Builder System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private ResourceSystem _resourceSystem;
    private WorldSystem _worldSystem;

    // Состояние режима стройки
    private bool _isPlacing;
    private BaseBuildingData _selectedBuilding;
    private GameObject _currentPreview;
    private SpriteRenderer _previewRenderer;
    private Camera _mainCamera;

    private LayerMask _buildingCollisionMask;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        _resourceSystem = _kernel.GetSystem<ResourceSystem>();
        _worldSystem = _kernel.GetSystem<WorldSystem>();

        _buildingCollisionMask = LayerMask.GetMask("Building", "Obstacle");
        _mainCamera = Camera.main;

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        if (!_isPlacing) return;

        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        // Блокируем клик, если курсор над элементом UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        UpdatePreviewPosition();
        UpdatePreviewVisuals();

        // Построиться по ЛКМ
        if (Input.GetMouseButtonDown(0) && CanPlaceBuilding())
        {
            PlaceBuilding();
        }

        // Отмена по ПКМ или Escape
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
    /// Запуск режима размещения выбранного здания.
    /// </summary>
    public void StartPlacement(BaseBuildingData buildingData)
    {
        if (buildingData == null) return;

        CancelBuilding(); // Отменяем текущее превью если было

        // Проверяем ресурсы локального игрока
        Player localPlayer = Mirror.NetworkClient.localPlayer != null
            ? Mirror.NetworkClient.localPlayer.GetComponent<Player>()
            : null;

        if (localPlayer != null && _resourceSystem != null)
        {
            if (!_resourceSystem.HasResources(localPlayer, buildingData.ConstructionCost))
            {
                Debug.LogWarning($"[{SystemName}] Недостаточно ресурсов для постройки {buildingData.DisplayName}");
                return;
            }
        }

        _selectedBuilding = buildingData;
        CreatePreview();
        _isPlacing = true;
    }

    public void CancelBuilding()
    {
        if (_currentPreview != null)
        {
            Object.Destroy(_currentPreview);
        }
        _isPlacing = false;
        _selectedBuilding = null;
        _currentPreview = null;
    }

    private void CreatePreview()
    {
        _currentPreview = new GameObject("BuildingPreview");
        _previewRenderer = _currentPreview.AddComponent<SpriteRenderer>();

        if (_selectedBuilding.Icon != null)
        {
            _previewRenderer.sprite = _selectedBuilding.Icon;
        }
        _previewRenderer.sortingOrder = 100;

        BoxCollider2D collider = _currentPreview.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
        collider.isTrigger = true;
    }

    private void UpdatePreviewPosition()
    {
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // Привязка к сетке 1x1 с учетом четности габаритов
        _currentPreview.transform.position = new Vector2(
            Mathf.Round(mousePos.x),
            Mathf.Round(mousePos.y)
        );
    }

    private void UpdatePreviewVisuals()
    {
        bool canBuild = CanPlaceBuilding();
        _previewRenderer.color = canBuild ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
    }

    /// <summary>
    /// Комплексная проверка: Ресурсы -> Правила -> Карта (Фундамент и Воздух) -> Физические коллизии.
    /// </summary>
    private bool CanPlaceBuilding()
    {
        if (_currentPreview == null || _selectedBuilding == null) return false;

        Vector2 pos = _currentPreview.transform.position;
        int width = _selectedBuilding.Width;
        int height = _selectedBuilding.Height;

        // 1. Проверка ресурсов
        Player localPlayer = Mirror.NetworkClient.localPlayer != null
            ? Mirror.NetworkClient.localPlayer.GetComponent<Player>()
            : null;

        if (localPlayer != null && _resourceSystem != null)
        {
            if (!_resourceSystem.HasResources(localPlayer, _selectedBuilding.ConstructionCost))
                return false;
        }

        // 2. Проверка правил размещения (Placement Rules)
        if (!CheckPlacementRules(pos)) return false;

        // 3. Проверка сетки блоков карты (Воздух внутри и Скала в фундаменте)
        if (!CheckBuildableOnMap(pos, width, height)) return false;

        // 4. Проверка пересечения с другими физическими зданиями
        if (!CheckNoBuildingCollision(pos, width, height)) return false;

        return true;
    }

    private bool CheckPlacementRules(Vector2 position)
    {
        if (_selectedBuilding.PlacementRules == null) return true;
        foreach (var rule in _selectedBuilding.PlacementRules)
        {
            if (rule != null && !rule.IsSatisfied(position))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Проверка карты: Все клетки прямоугольника здания должны быть Air, 
    /// а под нижним рядом должна быть твердая порода (Rock).
    /// </summary>
    private bool CheckBuildableOnMap(Vector2 position, int width, int height)
    {
        if (_worldSystem == null || _worldSystem.WorldMap == null) return false;

        int startX = Mathf.RoundToInt(position.x - width / 2f);
        int startY = Mathf.RoundToInt(position.y - height / 2f);
        int endX = startX + width - 1;
        int endY = startY + height - 1;

        // Проверяем всю площадь занимаемого объема (должен быть Air)
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                BlockType block = _worldSystem.GetBlockTypeAt(x, y);
                if (block != BlockType.Air)
                    return false;
            }
        }

        // Проверяем фундамент под самым нижним рядом постройки
        int groundY = startY - 1;
        for (int x = startX; x <= endX; x++)
        {
            BlockType below = _worldSystem.GetBlockTypeAt(x, groundY);
            if (below != BlockType.Rock)
                return false;
        }

        return true;
    }

    private bool CheckNoBuildingCollision(Vector2 position, int width, int height)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(position, new Vector2(width, height), 0, _buildingCollisionMask);
        foreach (var hit in hits)
        {
            if (hit.isTrigger || hit.gameObject == _currentPreview)
                continue;
            return false;
        }
        return true;
    }

    private void PlaceBuilding()
    {
        if (!CanPlaceBuilding() || _selectedBuilding == null) return;

        Vector2 pos = _currentPreview.transform.position;

        Player localPlayer = Mirror.NetworkClient.localPlayer != null
            ? Mirror.NetworkClient.localPlayer.GetComponent<Player>()
            : null;

        if (localPlayer == null || _resourceSystem == null) return;

        if (_resourceSystem.TrySpendResources(localPlayer, _selectedBuilding.ConstructionCost))
        {
            GameObject buildingObj = Object.Instantiate(_selectedBuilding.Prefab, pos, Quaternion.identity);
            buildingObj.layer = LayerMask.NameToLayer("Building");

            Building building = buildingObj.GetComponent<Building>();
            if (building != null)
            {
                _selectedBuilding.Owner = localPlayer;
                building.Initialize(_selectedBuilding);
                building.NotifyBuilt();
            }

            BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = buildingObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
            }

            // Если запущен сервер — спавним по сети Mirror
            if (Mirror.NetworkServer.active)
            {
                Mirror.NetworkServer.Spawn(buildingObj);
            }

            CancelBuilding();
        }
    }

    public bool IsPlacingBuilding() => _isPlacing;
}