using UnityEngine;
using UnityEngine.EventSystems;

public class BuildingSystem : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask obstacleLayer;
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

    [Header("Настройки проверки")]
    [SerializeField] private bool useChunkSystem = true;
    [SerializeField] private float checkPrecision = 0.5f;
    [SerializeField] private float minBuildablePercentage = 0.7f;

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

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

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

    private bool CanPlaceBuilding()
    {
        if (_currentPreview == null || _selectedBuildingData == null)
            return false;

        Vector2 pos = _currentPreview.transform.position;
        Vector2 size = new Vector2(_selectedBuildingData.Width, _selectedBuildingData.Height);

        if (!resourceManager.HasResources(_selectedBuildingData.ConstructionCost))
            return false;

        if (!CheckPlacementRules(pos))
            return false;

        if (!CheckSpaceClear(pos, size))
            return false;

        if (!CheckBuildableSurface(pos, size))
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

    private bool CheckSpaceClear(Vector2 position, Vector2 size)
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(position, size, 0, obstacleLayer);
        foreach (var col in overlaps)
        {
            if (col.isTrigger || col.gameObject == _currentPreview)
                continue;

            Block block = col.GetComponent<Block>();
            if (block != null && IsResourceBlock(block))
                return false;

            if (!col.isTrigger)
                return false;
        }
        return true;
    }

    private bool IsResourceBlock(Block block)
    {
        if (block == null) return false;
        string name = block.tileData.type.ToString().ToLower();
        return name.Contains("mineral") || name.Contains("ice") || name.Contains("root") || name.Contains("gold");
    }

    private bool CheckBuildableSurface(Vector2 position, Vector2 size)
    {
        if (useChunkSystem && worldManager != null)
            return CheckBuildableSurfaceUsingChunks(position, size);
        else
            return CheckBuildableSurfaceUsingRaycast(position, size);
    }

    private bool CheckBuildableSurfaceUsingChunks(Vector2 position, Vector2 size)
    {
        int valid = 0, total = 0;
        float step = checkPrecision;

        for (float x = -size.x / 2 + step / 2; x <= size.x / 2; x += step)
        {
            for (float y = -size.y / 2 + step / 2; y <= size.y / 2; y += step)
            {
                Vector2 point = position + new Vector2(x, y);
                total++;
                BlockType type = Map.Instance.GetBlockInfo(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)).tileData.type;
                if (IsBuildableBlockType(type))
                    valid++;
            }
        }
        return total > 0 && (float)valid / total >= minBuildablePercentage;
    }

    private bool IsBuildableBlockType(BlockType type)
    {
        if (type == BlockType.Air) return false;
        string name = type.ToString().ToLower();
        return name.Contains("rock") || name.Contains("dirt") || name.Contains("sand") || name.Contains("grass");
    }

    private bool CheckBuildableSurfaceUsingRaycast(Vector2 position, Vector2 size)
    {
        Vector2[] points = new Vector2[]
        {
            position + new Vector2(-size.x/2 + 0.1f, -size.y/2 + 0.1f),
            position + new Vector2(size.x/2 - 0.1f, -size.y/2 + 0.1f),
            position + new Vector2(0, -size.y/2 + 0.1f)
        };
        int valid = 0;
        foreach (var p in points)
        {
            RaycastHit2D hit = Physics2D.Raycast(p, Vector2.down, 2f, buildableLayer);
            if (hit.collider != null)
            {
                Block block = hit.collider.GetComponent<Block>();
                if (block != null && IsBuildableBlockType(block.tileData.type))
                    valid++;
            }
        }
        return valid >= 2;
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