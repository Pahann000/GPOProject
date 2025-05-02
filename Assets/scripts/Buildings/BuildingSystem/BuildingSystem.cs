using UnityEngine;

/// <summary>
/// ������� ������������� ������ � �������������� � ���������� ����������
/// </summary>
public class BuildingSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask buildableLayer; // ����, �� ������� ��������� �������������
    [SerializeField] private LayerMask obstacleLayer;  // ���� �����������

    [Header("Prefabs")]
    [SerializeField] private GameObject buildingPreviewPrefab; // ������ ��� �������������

    private GameObject _currentPreview;    // ������� ������ �������������
    private BuildingData _selectedBuilding; // ��������� ������ ��� ���������
    private bool _isPlacing;               // ���� ������ ����������

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

    /// <summary> ������ ������� ���������� ������ ������ </summary>
    public void StartBuildingPlacement(BuildingData buildingData)
    {
        _selectedBuilding = buildingData;
        CreatePreview();
        _isPlacing = true;
    }

    /// <summary> ������� ������ ������������� ������ </summary>
    private void CreatePreview()
    {
        _currentPreview = Instantiate(buildingPreviewPrefab);
        _currentPreview.GetComponent<SpriteRenderer>().sprite = _selectedBuilding.Icon;
    }

    /// <summary> �������� ������� ������������� �� ������� ���� </summary>
    private void UpdatePreviewPosition()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _currentPreview.transform.position = SnapToGrid(mousePos);
    }

    /// <summary> ��������� ������� � ����� </summary>
    private Vector2 SnapToGrid(Vector2 position)
    {
        float gridSize = 0.5f; // ������ ������ �����
        return new Vector2(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize
        );
    }

    /// <summary> �������� ���������� ����������� ������������� </summary>
    private void UpdatePreviewVisuals()
    {
        bool canBuild = CanPlaceBuilding();
        var renderer = _currentPreview.GetComponent<SpriteRenderer>();
        renderer.color = canBuild ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
    }

    /// <summary> ��������� ����������� ��������� � ������� ������� </summary>
    private bool CanPlaceBuilding()
    {
        return CheckPlacementRules() &&
               CheckResources() &&
               CheckSpaceClear();
    }

    /// <summary> ��������� ���������� ������ ���������� </summary>
    private bool CheckPlacementRules()
    {
        foreach (var rule in _selectedBuilding.PlacementRules)
        {
            if (!rule.IsSatisfied(_currentPreview.transform.position))
                return false;
        }
        return true;
    }

    /// <summary> ��������� ������� ����������� �������� </summary>
    private bool CheckResources()
    {
        return ResourceManager.Instance.HasResources(_selectedBuilding.ConstructionCost);
    }

    /// <summary> ��������� ���������� ����������� � ����� ������������� </summary>
    private bool CheckSpaceClear()
    {
        Collider2D overlap = Physics2D.OverlapBox(
            _currentPreview.transform.position,
            _currentPreview.GetComponent<BoxCollider2D>().size,
            0,
            obstacleLayer);
        return overlap == null;
    }

    /// <summary> ���������� ������ </summary>
    private void PlaceBuilding()
    {
        Instantiate(_selectedBuilding.Prefab, _currentPreview.transform.position, Quaternion.identity);
        ResourceManager.Instance.SpendResources(_selectedBuilding.ConstructionCost);
        CleanupPlacement();
    }

    /// <summary> �������� ������������� </summary>
    private void CancelBuilding()
    {
        CleanupPlacement();
    }

    /// <summary> �������� ��������� ������� ������������� </summary>
    private void CleanupPlacement()
    {
        Destroy(_currentPreview);
        _isPlacing = false;
        _selectedBuilding = null;
    }
}

