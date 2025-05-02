using UnityEngine;

/// <summary>
/// Система строительства зданий с предпросмотром и валидацией размещения
/// </summary>
public class BuildingSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask buildableLayer; // Слой, на котором разрешено строительство
    [SerializeField] private LayerMask obstacleLayer;  // Слой препятствий

    [Header("Prefabs")]
    [SerializeField] private GameObject buildingPreviewPrefab; // Префаб для предпросмотра

    private GameObject _currentPreview;    // Текущий объект предпросмотра
    private BuildingData _selectedBuilding; // Выбранное здание для постройки
    private bool _isPlacing;               // Флаг режима размещения

    void Update()
    {
        if (!_isPlacing) return;

        UpdatePreviewPosition();
        UpdatePreviewVisuals();

        if (Input.GetMouseButtonDown(0) && CanPlaceBuilding())
        {
            PlaceBuilding();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelBuilding();
        }
    }

    /// <summary> Начать процесс размещения нового здания </summary>
    public void StartBuildingPlacement(BuildingData buildingData)
    {
        _selectedBuilding = buildingData;
        CreatePreview();
        _isPlacing = true;
    }

    /// <summary> Создать объект предпросмотра здания </summary>
    private void CreatePreview()
    {
        _currentPreview = Instantiate(buildingPreviewPrefab);
        _currentPreview.GetComponent<SpriteRenderer>().sprite = _selectedBuilding.Icon;
    }

    /// <summary> Обновить позицию предпросмотра по курсору мыши </summary>
    private void UpdatePreviewPosition()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _currentPreview.transform.position = SnapToGrid(mousePos);
    }

    /// <summary> Привязать позицию к сетке </summary>
    private Vector2 SnapToGrid(Vector2 position)
    {
        float gridSize = 0.5f; // Размер клетки сетки
        return new Vector2(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize
        );
    }

    /// <summary> Обновить визуальное отображение предпросмотра </summary>
    private void UpdatePreviewVisuals()
    {
        bool canBuild = CanPlaceBuilding();
        var renderer = _currentPreview.GetComponent<SpriteRenderer>();
        renderer.color = canBuild ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
    }

    /// <summary> Проверить возможность постройки в текущей позиции </summary>
    private bool CanPlaceBuilding()
    {
        return CheckPlacementRules() &&
               CheckResources() &&
               CheckSpaceClear();
    }

    /// <summary> Проверить соблюдение правил размещения </summary>
    private bool CheckPlacementRules()
    {
        foreach (var rule in _selectedBuilding.PlacementRules)
        {
            if (!rule.IsSatisfied(_currentPreview.transform.position))
                return false;
        }
        return true;
    }

    /// <summary> Проверить наличие необходимых ресурсов </summary>
    private bool CheckResources()
    {
        return ResourceManager.Instance.HasResources(_selectedBuilding.ConstructionCost);
    }

    /// <summary> Проверить отсутствие препятствий в месте строительства </summary>
    private bool CheckSpaceClear()
    {
        Collider2D overlap = Physics2D.OverlapBox(
            _currentPreview.transform.position,
            _currentPreview.GetComponent<BoxCollider2D>().size,
            0,
            obstacleLayer);
        return overlap == null;
    }

    /// <summary> Разместить здание </summary>
    private void PlaceBuilding()
    {
        Instantiate(_selectedBuilding.Prefab, _currentPreview.transform.position, Quaternion.identity);
        ResourceManager.Instance.SpendResources(_selectedBuilding.ConstructionCost);
        CleanupPlacement();
    }

    /// <summary> Отменить строительство </summary>
    private void CancelBuilding()
    {
        CleanupPlacement();
    }

    /// <summary> Очистить состояние системы строительства </summary>
    private void CleanupPlacement()
    {
        Destroy(_currentPreview);
        _isPlacing = false;
        _selectedBuilding = null;
    }
}

