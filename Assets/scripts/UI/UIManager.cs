using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] public ResourcePanelUI resourcePanel;
    [SerializeField] public BuildingPanelUI buildingPanel;
    [SerializeField] public SelectionPanelUI selectionPanel;
    [SerializeField] public AdvancedTooltip tooltip;

    [Header("Input Settings")]
    [SerializeField] private KeyCode buildingMenuKey = KeyCode.B;
    [SerializeField] private KeyCode cancelKey = KeyCode.Escape;
    [SerializeField] private LayerMask selectableLayer = 1; // Default layer

    private Building selectedObject;
    private Camera mainCamera;
    private bool isInitialized = false;

    private void Start()
    {
        Debug.Log("UIManager: Start на " + gameObject.name);

        // Автоматический поиск компонентов если не назначены
        if (resourcePanel == null)
        {
            resourcePanel = FindFirstObjectByType<ResourcePanelUI>();
            Debug.Log($"ResourcePanel: {(resourcePanel != null ? "найден" : "не найден")}");
        }

        if (buildingPanel == null)
        {
            buildingPanel = FindFirstObjectByType<BuildingPanelUI>();
            Debug.Log($"BuildingPanel: {(buildingPanel != null ? "найден" : "не найден")}");
        }

        if (selectionPanel == null)
        {
            selectionPanel = FindFirstObjectByType<SelectionPanelUI>();
            Debug.Log($"SelectionPanel: {(selectionPanel != null ? "найден" : "не найден")}");
        }

        // Инициализация камеры
        InitializeCamera();

        // Проверка EventSystem
        CheckEventSystem();

        isInitialized = true;
        Debug.Log("UIManager: Инициализация завершена");
    }

    private void InitializeCamera()
    {
        // Ищем камеру, но не в Start, а в первом Update
        // Так как камера может быть еще не готова
    }

    private void CheckEventSystem()
    {
        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem не найден в сцене!");
            // Автоматическое создание EventSystem
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
            eventSystemGO.AddComponent<StandaloneInputModule>();
            Debug.Log("EventSystem создан автоматически");
        }
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Инициализируем камеру в первом кадре
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("MainCamera не найдена, пропускаем Update");
                return;
            }
            Debug.Log($"Камера инициализирована: {mainCamera.name}");
        }

        HandleInput();
        HandleSelection();
    }

    private void HandleInput()
    {
        // Открытие/закрытие меню строительства
        if (Input.GetKeyDown(buildingMenuKey))
        {
            ToggleBuildingMenu();
        }

        // Отмена выбора
        if (Input.GetKeyDown(cancelKey))
        {
            DeselectAll();
        }
    }

    private void HandleSelection()
    {
        //Debug.Log($"HandleSelection: EventSystem.current = {EventSystem.current}");
        //Debug.Log($"HandleSelection: mainCamera = {mainCamera}");

        if (EventSystem.current == null)
        {
            Debug.LogError("EventSystem.current is NULL!");
            return;
        }

        // Проверяем, не кликнули ли мы по UI элементу
        // Используем try-catch для безопасности
        try
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
        }
        catch (System.NullReferenceException)
        {
            // Если EventSystem.current стал null между проверками
            return;
        }

        if (Input.GetMouseButtonDown(0)) // Левая кнопка мыши
        {
            // Проверяем камеру
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // Безопасный Raycast
            try
            {
                RaycastHit2D hit = Physics2D.Raycast(
                    mainCamera.ScreenToWorldPoint(Input.mousePosition),
                    Vector2.zero,
                    Mathf.Infinity,
                    selectableLayer
                );

                if (hit.collider != null)
                {
                    Building building = hit.collider.GetComponent<Building>();
                    if (building != null)
                    {
                        SelectBuilding(building);
                    }
                    else
                    {
                        DeselectAll();
                    }
                }
                else
                {
                    DeselectAll();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка в HandleSelection: {e.Message}");
            }
        }
    }

    private void SelectBuilding(Building building)
    {
        if (building == null)
        {
            Debug.LogWarning("Попытка выбрать null building");
            return;
        }

        // Снимаем выделение с предыдущего объекта
        if (selectedObject != null && selectedObject != building)
        {
            // Здесь можно добавить визуальное снятие выделения
        }

        selectedObject = building;

        if (selectionPanel != null)
            selectionPanel.SelectBuilding(building);
        else
            Debug.LogWarning("SelectionPanel не назначен");

        Debug.Log($"Выбрано здание: {building.Data.DisplayName}");
    }

    private void DeselectAll()
    {
        selectedObject = null;

        if (selectionPanel != null)
            selectionPanel.Deselect();
    }

    private void ToggleBuildingMenu()
    {
        if (buildingPanel != null)
        {
            // Временно просто показываем/скрываем панель
            GameObject panelObject = buildingPanel.gameObject;
            panelObject.SetActive(!panelObject.activeSelf);
            Debug.Log($"Панель строительства: {(panelObject.activeSelf ? "открыта" : "закрыта")}");
        }
        else
        {
            Debug.LogWarning("BuildingPanel не назначен в UIManager");
        }
    }

    // Публичные методы для других систем
    public void ShowNotification(string message, float duration = 3f)
    {
        Debug.Log($"Уведомление: {message}");
    }

    public void UpdateAllUI()
    {
        Debug.Log("Обновление всего UI");
    }

    // Методы для Tooltip
    public void ShowTooltip(string title, string description, string requirements)
    {
        if (tooltip != null)
            tooltip.ShowTooltip(title, description, requirements);
    }

    public void HideTooltip()
    {
        if (tooltip != null)
            tooltip.HideTooltip();
    }
}