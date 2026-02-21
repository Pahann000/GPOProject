using UnityEngine;
using UnityEngine.EventSystems;

public class BuilderSystem : IGameSystem
{
    public string SystemName => "Builder System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private ResourceSystem _resourceSystem;

    // Состояние стройки
    private bool _isPlacing;
    private BuildingData _selectedBuilding;
    private GameObject _currentPreview;
    private SpriteRenderer _previewRenderer;
    private Camera _mainCamera;

    // В будущем эти настройки можно вынести в отдельный ScriptableObject (Config)
    private LayerMask _obstacleLayer;
    private Material _validMaterial;
    private Material _invalidMaterial;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        // Получаем нужные подсистемы сразу
        _resourceSystem = _kernel.GetSystem<ResourceSystem>();

        // Загружаем настройки из ресурсов (позже сделаем красивее через конфиги)
        _obstacleLayer = LayerMask.GetMask("Obstacle", "Building");

        // Пока ищем камеру по старинке. По-хорошему, нужна CameraSystem.
        _mainCamera = Camera.main;

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        if (!_isPlacing) return;

        // Если камера потерялась (например, при смене сцены), находим заново
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        // Проверка клика по UI (чтобы не строить сквозь кнопки)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        UpdatePreviewPosition();
        UpdatePreviewVisuals();

        // Обработка ввода
        if (Input.GetMouseButtonDown(0) && CanPlaceBuilding())
        {
            PlaceBuilding();
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

    // --- Логика Строительства (публичный API) ---

    public void StartPlacement(BuildingData buildingData)
    {
        if (buildingData == null) return;

        CancelBuilding(); // Сбрасываем предыдущую стройку

        if (!_resourceSystem.HasResources(buildingData.ConstructionCost))
        {
            Debug.Log($"[{SystemName}] Недостаточно ресурсов для {buildingData.DisplayName}");
            // TODO: Отправить событие в UI (через EventBus) "Ошибка: нет ресурсов"
            return;
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

    // --- Внутренние методы (урезанная версия твоего старого кода) ---

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

        // Привязка к сетке 1x1
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

    private bool CanPlaceBuilding()
    {
        if (_currentPreview == null || _selectedBuilding == null) return false;

        Vector2 checkPos = _currentPreview.transform.position;
        Vector2 checkSize = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);

        // 1. Ресурсы
        if (!_resourceSystem.HasResources(_selectedBuilding.ConstructionCost)) return false;

        // 2. Препятствия (упрощенная проверка)
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(checkPos, checkSize, 0, _obstacleLayer);
        foreach (var col in overlaps)
        {
            if (!col.isTrigger && col.gameObject != _currentPreview) return false;
        }

        var world = _kernel.GetSystem<WorldSystem>();
        if (world != null)
        {
            if (!world.IsSurfaceBuildable(checkPos, checkSize))
            {
                return false;
            }

            BlockType blockInside = world.GetBlockTypeAt(Mathf.Round)
        }

        return true;
    }

    private void PlaceBuilding()
    {
        Vector2 position = _currentPreview.transform.position;

        if (_resourceSystem.TrySpendResources(_selectedBuilding.ConstructionCost))
        {
            // Спавним настоящее здание
            GameObject buildingObj = Object.Instantiate(_selectedBuilding.Prefab, position, Quaternion.identity);

            Building building = buildingObj.GetComponent<Building>();
            if (building != null) building.Initialize(_selectedBuilding);

            BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = buildingObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
            }

            Debug.Log($"[{SystemName}] Здание {_selectedBuilding.DisplayName} построено!");

            // TODO: Отправить событие в EventBus "Здание построено"

            CancelBuilding();
        }
    }
}