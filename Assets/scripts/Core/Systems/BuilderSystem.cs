using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Управляет процессом размещения зданий на карте.
/// Обрабатывает пользовательский ввод, проверяет возможность постройки и списывает ресурсы.
/// </summary>
public class BuilderSystem : IGameSystem
{
    public string SystemName => "Builder System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private ResourceSystem _resourceSystem;

    // Состояние режима стройки
    private bool _isPlacing;
    private BuildingData _selectedBuilding;
    private GameObject _currentPreview;
    private SpriteRenderer _previewRenderer;
    private Camera _mainCamera;

    // TODO: Вынести настройки слоев и материалов в ScriptableObject, чтобы не "хардкодить" их имена в скрипте.
    private LayerMask _obstacleLayer;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        _resourceSystem = _kernel.GetSystem<ResourceSystem>();

        // Слой, на котором ищутся препятствия (другие здания)
        _obstacleLayer = LayerMask.GetMask("Obstacle", "Building");
        _mainCamera = Camera.main;

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        if (!_isPlacing) return;

        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        // Блокируем строительство, если мышка находится над кнопкой интерфейса
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        UpdatePreviewPosition();
        UpdatePreviewVisuals();

        if (Input.GetMouseButtonDown(0))
        {
            if (CanPlaceBuilding())
            {
                PlaceBuilding();
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
    /// <param name="buildingData">Данные о здании (цена, префаб, иконка).</param>
    public void StartPlacement(BuildingData buildingData)
    {
        if (buildingData == null) return;

        CancelBuilding(); // Сбрасываем предыдущую стройку

        if (!_resourceSystem.HasResources(buildingData.ConstructionCost))
        {
            // TODO: Вызвать событие "UINotificationEvent", чтобы показать всплывашку "Нет ресурсов".
            return;
        }

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
            Object.Destroy(_currentPreview);
        }
        _isPlacing = false;
        _selectedBuilding = null;
        _currentPreview = null;
    }

    /// <summary>
    /// Создает визуальную "голограмму" здания, которая следует за курсором.
    /// </summary>
    private void CreatePreview()
    {
        _currentPreview = new GameObject("BuildingPreview");
        _previewRenderer = _currentPreview.AddComponent<SpriteRenderer>();

        if (_selectedBuilding.Icon != null)
        {
            _previewRenderer.sprite = _selectedBuilding.Icon;
        }
        _previewRenderer.sortingOrder = 100;

        // Коллайдер нужен для физической проверки пересечений с препятствиями
        BoxCollider2D collider = _currentPreview.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
        collider.isTrigger = true;
    }

    /// <summary>
    /// Перемещает превью здания за курсором с привязкой к сетке (Snap to Grid).
    /// </summary>
    private void UpdatePreviewPosition()
    {
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);

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
        bool canBuild = CanPlaceBuilding();
        _previewRenderer.color = canBuild ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
    }

    /// <summary>
    /// Выполняет полную проверку: Ресурсы -> Препятствия -> Почва.
    /// </summary>
    private bool CanPlaceBuilding()
    {
        if (_currentPreview == null || _selectedBuilding == null) return false;

        Vector2 checkPos = _currentPreview.transform.position;
        Vector2 checkSize = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);

        // 1. Проверка ресурсов
        if (!_resourceSystem.HasResources(_selectedBuilding.ConstructionCost)) return false;

        // 2. Проверка препятствий (пересечение с другими зданиями)
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(checkPos, checkSize, 0, _obstacleLayer);
        foreach (var col in overlaps)
        {
            if (!col.isTrigger && col.gameObject != _currentPreview) return false;
        }

        // 3. Проверка взаимодействия с миром (опора под зданием)
        var world = _kernel.GetSystem<WorldSystem>();
        if (world != null)
        {
            if (!world.IsSurfaceBuildable(checkPos, checkSize))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Окончательное размещение здания: списание ресурсов и создание объекта на сцене.
    /// </summary>
    private void PlaceBuilding()
    {
        Vector2 position = _currentPreview.transform.position;

        if (_resourceSystem.TrySpendResources(_selectedBuilding.ConstructionCost))
        {
            GameObject buildingObj = Object.Instantiate(_selectedBuilding.Prefab, position, Quaternion.identity);

            // Жестко задаем слой, чтобы SelectionSystem мог его найти
            buildingObj.layer = LayerMask.NameToLayer("Building");

            Building building = buildingObj.GetComponent<Building>();
            if (building != null) building.Initialize(_selectedBuilding);

            BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                collider = buildingObj.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
            }

            // TODO: Отправить событие "BuildingPlacedEvent" в EventBus для звуков и эффектов
            CancelBuilding();
        }
    }

    public bool IsPlacingBuilding() => _isPlacing;
}