using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionSystem : IGameSystem
{
    public string SystemName => "Selection System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private Camera _mainCamera;
    private LayerMask _buildingLayer;

    private Building _selectedBuilding;

    public void Initialize(GameKernel kenel)
    {
        _kernel = kenel;
        _mainCamera = Camera.main;

        _buildingLayer = LayerMask.GetMask("Building");

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        var builder = _kernel.GetSystem<BuilderSystem>();
        if (builder != null && builder.IsPlacingBuilding()) return;

        // Отмена выделения (пока Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    public void FixedTick(float deltaTime) { }

    public void Shutdown()
    {
        Deselect();
    }

    private void HandleClick()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, _buildingLayer);

        if (hit.collider != null)
        {
            Building building = hit.collider.GetComponent<Building>();
            if (building != null)
            {
                SelectBuilding(building);
                return;
            }
        }

        Deselect();
    }

    private void SelectBuilding(Building building)
    {
        if (_selectedBuilding == building) return;
        
        if (_selectedBuilding != null)
        {
            _selectedBuilding.Deselect();
        }

        _selectedBuilding = building;
        _selectedBuilding.Select();

        _kernel.EventBus.Raise(new BuildingSelectedEvent(_selectedBuilding));

        Debug.Log($"[{SystemName}] Выбрано: {building.Data.DisplayName}");
    }

    public void Deselect()
    {
        if (_selectedBuilding != null)
        {
            _selectedBuilding.Deselect();
            _selectedBuilding = null;

            _kernel.EventBus.Raise(new BuildingDeselectedEvent());

            Debug.Log($"[{SystemName}] Выделение снято.");
        }
    }
}