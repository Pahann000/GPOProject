using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingSystem : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private LayerMask buildingCollisionMask; // маска для столкновений с другими зданиями
    [SerializeField] private GameObject buildingPreviewPrefab;
    [SerializeField] private Material validMaterial;
    [SerializeField] private Material invalidMaterial;

    [Header("Ссылки")]
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private WorldManager worldManager;

    private GameObject _currentPreview;
    private BaseBuildingData _selectedBuildingData;
    private bool _isPlacing;
    private SpriteRenderer _previewRenderer;
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        if (resourceManager == null) resourceManager = FindFirstObjectByType<ResourceManager>();
        if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();
        if (worldManager == null) worldManager = FindFirstObjectByType<WorldManager>();
    }

    private void Update()
    {
        if (!_isPlacing) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        UpdatePreviewPosition();
        UpdatePreviewVisuals();

        if (Input.GetMouseButtonDown(0) && CanPlaceBuilding())
            PlaceBuilding();

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelBuilding();
    }

    public void StartBuildingPlacement(BaseBuildingData buildingData)
    {
        if (_isPlacing) CancelBuilding();

        if (!resourceManager.HasResources(buildingData.ConstructionCost))
        {
            uiManager?.ShowNotification($"Недостаточно ресурсов для {buildingData.DisplayName}");
            return;
        }

        _selectedBuildingData = buildingData;
        CreatePreview();
        _isPlacing = true;
        uiManager?.ShowNotification($"Размещайте {buildingData.DisplayName}. ЛКМ - построить, ПКМ - отмена");
    }

    private void CreatePreview()
    {
        if (buildingPreviewPrefab != null)
        {
            _currentPreview = Instantiate(buildingPreviewPrefab);
        }
        else
        {
            _currentPreview = new GameObject("BuildingPreview");
            _previewRenderer = _currentPreview.AddComponent<SpriteRenderer>();
            if (_selectedBuildingData.Icon != null)
                _previewRenderer.sprite = _selectedBuildingData.Icon;
        }

        _previewRenderer = _currentPreview.GetComponent<SpriteRenderer>();
        if (_previewRenderer != null)
            _previewRenderer.sortingOrder = 100;

        SetupPreviewCollider();
    }

    private void SetupPreviewCollider()
    {
        var old = _currentPreview.GetComponent<Collider2D>();
        if (old != null) Destroy(old);

        var collider = _currentPreview.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(_selectedBuildingData.Width, _selectedBuildingData.Height);
        collider.isTrigger = true;
    }

    private void UpdatePreviewPosition()
    {
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _currentPreview.transform.position = SnapToGrid(mousePos);
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float gridSize = 1f;
        return new Vector2(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize
        );
    }

    private void UpdatePreviewVisuals()
    {
        if (_previewRenderer == null) return;

        bool canBuild = CanPlaceBuilding();
        _previewRenderer.color = canBuild ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);

        if (validMaterial != null && invalidMaterial != null)
            _previewRenderer.material = canBuild ? validMaterial : invalidMaterial;
    }

    // ======================== НОВЫЕ МЕТОДЫ ПРОВЕРКИ ========================

    private bool CanPlaceBuilding()
    {
        if (_currentPreview == null || _selectedBuildingData == null)
            return false;

        Vector2 pos = _currentPreview.transform.position;
        int width = _selectedBuildingData.Width;
        int height = _selectedBuildingData.Height;

        // 1. Проверка ресурсов
        if (!resourceManager.HasResources(_selectedBuildingData.ConstructionCost))
            return false;

        // 2. Проверка правил размещения (например, рядом с водой)
        if (!CheckPlacementRules(pos))
            return false;

        // 3. Проверка блоков через Map (фундамент и свободное пространство)
        if (!CheckBuildableOnMap(pos, width, height))
            return false;

        // 4. Проверка столкновения с другими зданиями
        if (!CheckNoBuildingCollision(pos, width, height))
            return false;

        return true;
    }

    private bool CheckPlacementRules(Vector2 position)
    {
        if (_selectedBuildingData.PlacementRules == null) return true;
        foreach (var rule in _selectedBuildingData.PlacementRules)
            if (rule != null && !rule.IsSatisfied(position))
                return false;
        return true;
    }

    /// <summary>
    /// Проверка через Map: все клетки здания должны быть Air, под нижней гранью – твёрдый блок.
    /// </summary>
    private bool CheckBuildableOnMap(Vector2 position, int width, int height)
    {
        // Определяем целочисленные координаты левого нижнего угла (в клетках)
        int startX = Mathf.RoundToInt(position.x - width / 2f);
        int startY = Mathf.RoundToInt(position.y - height / 2f);
        int endX = startX + width - 1;
        int endY = startY + height - 1;

        // Проходим по всем клеткам, которые занимает здание
        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                BlockType block = Map.Instance.GetBlockInfo(x, y).tileData.type;
                if (block != BlockType.Air)
                    return false;
            }
        }

        // Проверяем, что под каждой клеткой нижнего ряда есть твёрдый строительный блок
        int groundY = startY - 1;
        for (int x = startX; x <= endX; x++)
        {
            BlockType below = Map.Instance.GetBlockInfo(x, groundY).tileData.type;
            if (!IsBuildableBlockType(below))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Проверка пересечения с другими зданиями через физику.
    /// </summary>
    private bool CheckNoBuildingCollision(Vector2 position, int width, int height)
    {
        Collider2D[] hits = Physics2D.OverlapBoxAll(position, new Vector2(width, height), 0, buildingCollisionMask);
        foreach (var hit in hits)
        {
            if (hit.isTrigger || hit.gameObject == _currentPreview)
                continue;
            // Если это другое здание или любой объект с коллайдером не-триггером – запрещаем
            return false;
        }
        return true;
    }

    private bool IsBuildableBlockType(BlockType type)
    {
        // Разрешаем строить на камне, земле, песке, траве (и любых других твёрдых блоках, кроме ресурсов и воздуха)
        return type == BlockType.Rock;
    }

    private void PlaceBuilding()
    {
        if (!CanPlaceBuilding() || _selectedBuildingData == null) return;

        Vector2 pos = _currentPreview.transform.position;

        if (resourceManager.TrySpendResources(_selectedBuildingData.ConstructionCost))
        {
            Building building = worldManager?.PlaceBuilding(_selectedBuildingData, pos);
            if (building != null)
            {
                building.Initialize(_selectedBuildingData);
                building.NotifyBuilt();
            }
            else
            {
                CreateBuildingDirectly(pos);
            }
            CreateConstructionEffect(pos);
            uiManager?.ShowNotification($"{_selectedBuildingData.DisplayName} построен!");
            CleanupPlacement();
        }
        else
        {
            uiManager?.ShowNotification("Не удалось списать ресурсы!");
        }
    }

    private void CreateBuildingDirectly(Vector2 position)
    {
        GameObject obj = Instantiate(_selectedBuildingData.Prefab, position, Quaternion.identity);
        Building building = obj.GetComponent<Building>();
        if (building != null)
        {
            building.Initialize(_selectedBuildingData);
            building.NotifyBuilt();
        }

        BoxCollider2D col = obj.GetComponent<BoxCollider2D>();
        if (col == null)
        {
            col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(_selectedBuildingData.Width, _selectedBuildingData.Height);
        }
    }

    private void CreateConstructionEffect(Vector3 position)
    {
        GameObject effect = new GameObject("ConstructionEffect");
        effect.transform.position = position;
        SpriteRenderer r = effect.AddComponent<SpriteRenderer>();
        r.sprite = _selectedBuildingData.Icon;
        r.color = new Color(1, 1, 1, 0.5f);
        r.sortingOrder = 99;
        Destroy(effect, 1f);
    }

    private void CancelBuilding()
    {
        if (_currentPreview != null) Destroy(_currentPreview);
        _isPlacing = false;
        _selectedBuildingData = null;
        _currentPreview = null;
        uiManager?.ShowNotification("Строительство отменено");
    }

    private void CleanupPlacement()
    {
        if (_currentPreview != null) Destroy(_currentPreview);
        _isPlacing = false;
        _selectedBuildingData = null;
        _currentPreview = null;
    }

    public bool IsPlacingBuilding() => _isPlacing;
    public BaseBuildingData GetSelectedBuilding() => _selectedBuildingData;
    public Vector3 GetPreviewPosition() => _currentPreview != null ? _currentPreview.transform.position : Vector3.zero;
}