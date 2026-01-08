using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
    [SerializeField] private WorldManager worldManager; // Добавляем ссылку на WorldManager

    private GameObject _currentPreview;
    private BuildingData _selectedBuilding;
    private bool _isPlacing;
    private SpriteRenderer _previewRenderer;
    private Camera _mainCamera;

    private Dictionary<ResourceType, int> _availableResources = new Dictionary<ResourceType, int>();

    [Header("Настройки проверки чанков")]
    [SerializeField] private bool useChunkSystem = true;
    [SerializeField] private float checkPrecision = 0.5f;
    [SerializeField] private float minBuildablePercentage = 0.7f; // Минимум 70% площади должно быть на пригодной поверхности

    void Start()
    {
        _mainCamera = Camera.main;

        if (resourceManager == null)
            resourceManager = FindObjectOfType<ResourceManager>();
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();
        if (worldManager == null)
            worldManager = FindObjectOfType<WorldManager>(); // Автопоиск WorldManager

        InitializeResourceTracking();
    }

    void Update()
    {
        if (!_isPlacing) return;

        // Проверка клика по UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        UpdatePreviewPosition();
        UpdatePreviewVisuals();

        if (Input.GetMouseButtonDown(0) && CanPlaceBuilding())
        {
            PlaceBuilding();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBuilding();
        }

        // Быстрая проверка ресурсов каждый кадр
        UpdateResourceAvailability();
    }

    private void InitializeResourceTracking()
    {
        // Подписываемся на события изменения ресурсов
        if (resourceManager != null)
        {
            ResourceManager.OnResourceChanged += OnResourceChanged;

            // Инициализируем словарь ресурсов
            foreach (ResourceType resourceType in System.Enum.GetValues(typeof(ResourceType)))
            {
                _availableResources[resourceType] = resourceManager.GetResource(resourceType);
            }
        }
    }

    private void OnResourceChanged(ResourceType type)
    {
        // Обновляем локальный кэш ресурсов
        if (resourceManager != null)
        {
            _availableResources[type] = resourceManager.GetResource(type);
        }
    }

    public void StartBuildingPlacement(BuildingData buildingData)
    {
        Debug.Log($"=== НАЧАЛО СТРОИТЕЛЬСТВА: {buildingData.DisplayName} ===");

        // Если уже размещаем, отменяем
        if (_isPlacing)
            CancelBuilding();

        // Проверяем ресурсы
        if (!HasEnoughResources(buildingData.ConstructionCost))
        {
            uiManager?.ShowNotification($"Недостаточно ресурсов для {buildingData.DisplayName}");
            return;
        }

        _selectedBuilding = buildingData;
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
            SpriteRenderer renderer = _currentPreview.AddComponent<SpriteRenderer>();
            if (_selectedBuilding.Icon != null)
            {
                renderer.sprite = _selectedBuilding.Icon;
                renderer.color = new Color(0, 1, 0, 0.7f);
            }
        }

        _previewRenderer = _currentPreview.GetComponent<SpriteRenderer>();
        if (_previewRenderer != null)
        {
            if (_selectedBuilding.Icon != null && _previewRenderer.sprite == null)
                _previewRenderer.sprite = _selectedBuilding.Icon;
            _previewRenderer.sortingOrder = 100;
        }

        SetupPreviewCollider();
        UpdateResourceAvailability();
    }

    private void SetupPreviewCollider()
    {
        // Удаляем старый коллайдер если есть
        Collider2D oldCollider = _currentPreview.GetComponent<Collider2D>();
        if (oldCollider != null) Destroy(oldCollider);

        BoxCollider2D collider = _currentPreview.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
        collider.isTrigger = true;
    }

    private void UpdatePreviewPosition()
    {
        if (_currentPreview == null) return;

        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _currentPreview.transform.position = SnapToGrid(mousePos);
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float gridSize = 1f; // Размер сетки
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

        // Можно использовать материалы для более сложных эффектов
        if (validMaterial != null && invalidMaterial != null)
        {
            _previewRenderer.material = canBuild ? validMaterial : invalidMaterial;
        }
    }

    private void UpdateResourceAvailability()
    {
        if (_selectedBuilding != null && resourceManager != null)
        {
            bool hasResources = HasEnoughResources(_selectedBuilding.ConstructionCost);
            // Визуальная индикация недостатка ресурсов
            if (!hasResources && _previewRenderer != null)
            {
                _previewRenderer.color = new Color(1, 0.5f, 0, 0.7f); // Оранжевый - недостаточно ресурсов
            }
        }
    }

    private bool CanPlaceBuilding()
    {
        if (_currentPreview == null || _selectedBuilding == null)
            return false;

        Vector2 checkPos = _currentPreview.transform.position;
        Vector2 checkSize = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);

        Debug.Log($"=== ПРОВЕРКА РАЗМЕЩЕНИЯ НА {checkPos} ===");

        // 1. Проверка ресурсов
        if (!HasEnoughResources(_selectedBuilding.ConstructionCost))
        {
            Debug.Log("❌ Недостаточно ресурсов");
            return false;
        }

        // 2. Проверка правил размещения
        if (!CheckPlacementRules(checkPos))
        {
            Debug.Log("❌ Не пройдены правила размещения");
            return false;
        }

        // 3. Проверка свободного пространства (препятствия)
        if (!CheckSpaceClear(checkPos, checkSize))
        {
            Debug.Log("❌ Пространство занято препятствиями");
            return false;
        }

        // 4. Проверка на строительной поверхности
        if (!CheckBuildableSurface(checkPos, checkSize))
        {
            Debug.Log("❌ Нет подходящей поверхности");
            return false;
        }

        Debug.Log("✅ Все проверки пройдены");
        return true;
    }

    private bool HasEnoughResources(ResourceBundle cost)
    {
        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager не найден!");
            return false;
        }
        return resourceManager.HasResources(cost);
    }

    private bool CheckPlacementRules(Vector2 position)
    {
        if (_selectedBuilding.PlacementRules == null || _selectedBuilding.PlacementRules.Count == 0)
            return true;

        foreach (PlacementRule rule in _selectedBuilding.PlacementRules)
        {
            if (rule != null && !rule.IsSatisfied(position))
            {
                Debug.Log($"Правило размещения не выполнено: {rule.name}");
                return false;
            }
        }
        return true;
    }

    private bool CheckSpaceClear(Vector2 position, Vector2 size)
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            position,
            size,
            0,
            obstacleLayer
        );

        int validObstacles = 0;
        foreach (Collider2D col in overlaps)
        {
            // Игнорируем триггеры и сам превью
            if (col.isTrigger || col.gameObject == _currentPreview)
                continue;

            // Проверяем, является ли объект ресурсным блоком
            Block block = col.GetComponent<Block>();
            if (block != null)
            {
                // Если это ресурсный блок (золото, корни, лед) - нельзя строить
                if (IsResourceBlock(block))
                {
                    Debug.Log($"Найден ресурсный блок: {block.BlockType?.Name}");
                    validObstacles++;
                }
                else if (block.BlockType != null && block.BlockType.Name.ToLower().Contains("rock"))
                {
                    // Камень - можно строить (но не является препятствием сам по себе)
                    continue;
                }
            }

            // Другие коллайдеры (здания и т.д.)
            if (!col.isTrigger)
            {
                Debug.Log($"Найдено препятствие: {col.name}");
                validObstacles++;
            }
        }

        return validObstacles == 0;
    }

    private bool CheckBuildableSurface(Vector2 position, Vector2 size)
    {
        // Если используем систему чанков через WorldManager
        if (useChunkSystem && worldManager != null)
        {
            return CheckBuildableSurfaceUsingChunks(position, size);
        }
        else
        {
            // Старая проверка через Raycast (для статической земли)
            return CheckBuildableSurfaceUsingRaycast(position, size);
        }
    }

    private bool CheckBuildableSurfaceUsingChunks(Vector2 position, Vector2 size)
    {
        Debug.Log("=== ПРОВЕРКА ПОВЕРХНОСТИ ЧЕРЕЗ СИСТЕМУ ЧАНКОВ ===");

        if (worldManager == null)
        {
            Debug.LogError("WorldManager не найден!");
            return false;
        }

        // Проверяем сетку точек под зданием
        int validPoints = 0;
        int totalPoints = 0;

        // Шаг проверки (можно настроить)
        float step = checkPrecision;

        for (float x = -size.x / 2 + step / 2; x <= size.x / 2 - step / 2; x += step)
        {
            for (float y = -size.y / 2 + step / 2; y <= size.y / 2 - step / 2; y += step)
            {
                Vector2 checkPoint = position + new Vector2(x, y);
                totalPoints++;

                // Получаем тип блока в этой позиции
                BlockType blockType = worldManager.GetBlockTypeAt(
                    Mathf.RoundToInt(checkPoint.x),
                    Mathf.RoundToInt(checkPoint.y)
                );

                if (blockType != null)
                {
                    // Проверяем, можно ли строить на этом типе блока
                    if (IsBuildableBlockType(blockType))
                    {
                        validPoints++;
                        Debug.Log($"Точка {checkPoint}: {blockType.Name} - МОЖНО СТРОИТЬ");
                    }
                    else
                    {
                        Debug.Log($"Точка {checkPoint}: {blockType.Name} - НЕЛЬЗЯ СТРОИТЬ");
                    }
                }
                else
                {
                    Debug.Log($"Точка {checkPoint}: Блок не найден (возможно, пустота)");
                }
            }
        }

        // Вычисляем процент пригодной поверхности
        float percentage = totalPoints > 0 ? (float)validPoints / totalPoints : 0f;
        Debug.Log($"Результат: {validPoints}/{totalPoints} точек ({percentage:P0})");

        return percentage >= minBuildablePercentage;
    }

    private bool CheckBuildableSurfaceUsingRaycast(Vector2 position, Vector2 size)
    {
        Debug.Log("=== ПРОВЕРКА ПОВЕРХНОСТИ ЧЕРЕЗ RAYCAST ===");

        // Проверяем несколько точек под зданием
        Vector2[] checkPoints = new Vector2[]
        {
            position + new Vector2(-size.x/2 + 0.1f, -size.y/2 + 0.1f),
            position + new Vector2(size.x/2 - 0.1f, -size.y/2 + 0.1f),
            position + new Vector2(0, -size.y/2 + 0.1f)
        };

        int validPoints = 0;
        foreach (Vector2 point in checkPoints)
        {
            RaycastHit2D hit = Physics2D.Raycast(point, Vector2.down, 2f, buildableLayer);
            if (hit.collider != null)
            {
                // Проверяем, можно ли строить на этом блоке
                Block block = hit.collider.GetComponent<Block>();
                if (block != null && block.BlockType != null)
                {
                    if (IsBuildableBlockType(block.BlockType))
                        validPoints++;
                }
            }
        }

        // Требуется хотя бы 2 точки на строительной поверхности
        return validPoints >= 2;
    }

    // Определяет, можно ли строить на данном типе блока
    private bool IsBuildableBlockType(BlockType blockType)
    {
        if (blockType == null) return false;

        string blockName = blockType.Name.ToLower();

        // Можно строить на камне, грязи, песке, земле
        return blockName.Contains("rock") ||
               blockName.Contains("dirt") ||
               blockName.Contains("sand") ||
               blockName.Contains("grass") ||
               blockName.Contains("земля") ||
               blockName.Contains("камень");
    }

    // Определяет, является ли блок ресурсом (нельзя строить)
    private bool IsResourceBlock(Block block)
    {
        if (block == null || block.BlockType == null) return false;

        string blockName = block.BlockType.Name.ToLower();
        return blockName.Contains("mineral") ||
               blockName.Contains("ice") ||
               blockName.Contains("root") ||
               blockName.Contains("gold") ||
               blockName.Contains("золото") ||
               blockName.Contains("лед") ||
               blockName.Contains("корень");
    }

    private void PlaceBuilding()
    {
        if (!CanPlaceBuilding() || _selectedBuilding == null) return;

        Vector2 position = _currentPreview.transform.position;
        Debug.Log($"=== ПОПЫТКА ПОСТРОИТЬ {_selectedBuilding.DisplayName} НА {position} ===");

        // Списываем ресурсы
        if (resourceManager.TrySpendResources(_selectedBuilding.ConstructionCost))
        {
            // Используем WorldManager для размещения здания
            Building building = worldManager?.PlaceBuilding(_selectedBuilding, position);

            if (building != null)
            {
                // Создаем здание через WorldManager
                CreateConstructionEffect(position);

                uiManager?.ShowNotification($"{_selectedBuilding.DisplayName} построен!");
                Debug.Log($"✅ Здание успешно построено!");

                // Очищаем
                CleanupPlacement();
            }
            else
            {
                // Если WorldManager не смог разместить, пытаемся создать напрямую
                Debug.LogWarning("WorldManager не разместил здание, создаем напрямую");
                CreateBuildingDirectly(position);
            }
        }
        else
        {
            uiManager?.ShowNotification("Не удалось списать ресурсы!");
            Debug.LogError("❌ Не удалось списать ресурсы!");
        }
    }

    private void CreateBuildingDirectly(Vector2 position)
    {
        // Создаем здание
        GameObject buildingObj = Instantiate(
            _selectedBuilding.Prefab,
            position,
            Quaternion.identity
        );

        // Инициализируем здание
        Building building = buildingObj.GetComponent<Building>();
        if (building != null)
        {
            building.Initialize(_selectedBuilding);
        }

        // Добавляем коллайдер
        BoxCollider2D collider = buildingObj.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = buildingObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
        }

        CreateConstructionEffect(position);
        CleanupPlacement();
    }

    private void CreateConstructionEffect(Vector3 position)
    {
        // Простой эффект строительства
        GameObject effect = new GameObject("ConstructionEffect");
        effect.transform.position = position;

        SpriteRenderer effectRenderer = effect.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = _selectedBuilding.Icon;
        effectRenderer.color = new Color(1, 1, 1, 0.5f);
        effectRenderer.sortingOrder = 99;

        // Анимация исчезновения
        Destroy(effect, 1f);
    }

    private void CancelBuilding()
    {
        if (_currentPreview != null)
        {
            Destroy(_currentPreview);
        }

        _isPlacing = false;
        _selectedBuilding = null;
        _currentPreview = null;

        uiManager?.ShowNotification("Строительство отменено");
        Debug.Log("Строительство отменено");
    }

    private void CleanupPlacement()
    {
        if (_currentPreview != null)
        {
            Destroy(_currentPreview);
        }

        _isPlacing = false;
        _selectedBuilding = null;
        _currentPreview = null;
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
        {
            ResourceManager.OnResourceChanged -= OnResourceChanged;
        }
    }

    // Методы для UI
    public bool IsPlacingBuilding() => _isPlacing;
    public BuildingData GetSelectedBuilding() => _selectedBuilding;
    public Vector3 GetPreviewPosition() => _currentPreview != null ? _currentPreview.transform.position : Vector3.zero;

    // Для отладки в редакторе
    void OnDrawGizmosSelected()
    {
        if (!_isPlacing || _currentPreview == null || _selectedBuilding == null)
            return;

        Vector2 position = _currentPreview.transform.position;
        Vector2 size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);

        // Рисуем контур здания
        Gizmos.color = CanPlaceBuilding() ? Color.green : Color.red;
        Gizmos.DrawWireCube(position, size);

        // Рисуем точки проверки (для системы чанков)
        if (useChunkSystem && worldManager != null)
        {
            Gizmos.color = Color.yellow;
            float step = checkPrecision;

            for (float x = -size.x / 2 + step / 2; x <= size.x / 2 - step / 2; x += step)
            {
                for (float y = -size.y / 2 + step / 2; y <= size.y / 2 - step / 2; y += step)
                {
                    Vector2 checkPoint = position + new Vector2(x, y);
                    Gizmos.DrawSphere(checkPoint, 0.05f);
                }
            }
        }
    }
}