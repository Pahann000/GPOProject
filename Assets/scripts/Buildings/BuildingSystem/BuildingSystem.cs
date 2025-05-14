using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LayerMask buildableLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private GameObject buildingPreviewPrefab;

    private GameObject _currentPreview;
    private BuildingData _selectedBuilding;
    private bool _isPlacing;

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

    public void StartBuildingPlacement(BuildingData buildingData)
    {
        _selectedBuilding = buildingData;
        CreatePreview();
        _isPlacing = true;
    }

    private void CreatePreview()
    {
        _currentPreview = Instantiate(buildingPreviewPrefab);
        _currentPreview.GetComponent<SpriteRenderer>().sprite = _selectedBuilding.Icon;
        SetupPreviewCollider();
    }

    private void SetupPreviewCollider()
    {
        BoxCollider2D collider = _currentPreview.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(_selectedBuilding.Width, _selectedBuilding.Height);
        collider.isTrigger = true;
    }

    private void UpdatePreviewPosition()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _currentPreview.transform.position = SnapToGrid(mousePos);
    }

    private Vector2 SnapToGrid(Vector2 position)
    {
        float gridSize = 0.5f;
        return new Vector2(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize
        );
    }

    private void UpdatePreviewVisuals()
    {
        bool canBuild = CanPlaceBuilding();
        var renderer = _currentPreview.GetComponent<SpriteRenderer>();
        renderer.color = canBuild ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
    }

    private bool CanPlaceBuilding()
    {
        return CheckPlacementRules() &&
               CheckResources() &&
               CheckSpaceClear();
    }

    private bool CheckPlacementRules()
    {
        foreach (var rule in _selectedBuilding.PlacementRules)
        {
            if (!rule.IsSatisfied(_currentPreview.transform.position))
                return false;
        }
        return true;
    }

    private bool CheckResources()
    {
        return ResourceManager.Instance.HasResources(_selectedBuilding.ConstructionCost);
    }

    private bool CheckSpaceClear()
    {
        Collider2D[] overlaps = Physics2D.OverlapBoxAll(
            _currentPreview.transform.position,
            new Vector2(_selectedBuilding.Width, _selectedBuilding.Height),
            0,
            obstacleLayer
        );

        foreach (Collider2D col in overlaps)
        {
            if (col.gameObject != _currentPreview)
                return false;
        }
        return true;
    }

    private void PlaceBuilding()
    {
        if (ResourceManager.Instance.TrySpendResources(_selectedBuilding.ConstructionCost))
        {
            Instantiate(_selectedBuilding.Prefab, _currentPreview.transform.position, Quaternion.identity);
            CleanupPlacement();
        }
    }

    private void CancelBuilding()
    {
        CleanupPlacement();
    }

    private void CleanupPlacement()
    {
        Destroy(_currentPreview);
        _isPlacing = false;
        _selectedBuilding = null;
    }
}