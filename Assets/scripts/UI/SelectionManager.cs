using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager Instance { get; private set; }

    private Building _selectedBuilding;
    private Camera _mainCamera;

    [SerializeField] private LayerMask _buildingLayer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
        }
    }

    private void HandleSelection()
    {
        // Проверяем, не кликнули ли по UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

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

    public void SelectBuilding(Building building)
    {
        // Снимаем выделение с предыдущего
        if (_selectedBuilding != null && _selectedBuilding != building)
        {
            _selectedBuilding.Deselect();
        }

        _selectedBuilding = building;
        _selectedBuilding.Select();

        // Показываем информацию в UI
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null && building.Data != null)
        {
            string info = $"{building.Data.DisplayName}\nЗдоровье: {building.CurrentHealth}/{building.Data.MaxHealth}";
            uiManager.ShowNotification(info, 3f);
        }

        Debug.Log($"Выбрано: {building.Data.DisplayName}");
    }

    private void Deselect()
    {
        if (_selectedBuilding != null)
        {
            _selectedBuilding.Deselect();
            _selectedBuilding = null;
        }
    }

    public Building GetSelectedBuilding() => _selectedBuilding;
}