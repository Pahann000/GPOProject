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
        Debug.Log("--- НАЧАЛО КЛИКА ---");

        // 1. Проверка Камеры
        if (_mainCamera == null)
        {
            Debug.LogError("ОШИБКА: _mainCamera is NULL! Ядро не нашло камеру.");
            return;
        }

        // 2. Проверка UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Клик заблокирован интерфейсом (UI).");
            // Попробуем узнать, какой именно элемент UI перекрывает экран
            GameObject uiObject = EventSystem.current.currentSelectedGameObject;
            // К сожалению, currentSelectedGameObject не всегда показывает то, что под мышкой
            // Но сам факт срабатывания условия говорит о том, что Raycast Target на Canvas мешает.
            return;
        }

        // 3. Пускаем луч БЕЗ маски слоя (чтобы увидеть вообще всё, что под мышкой)
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // Рисуем луч в редакторе (будет виден в окне Scene красной линией 2 секунды)
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f);

        RaycastHit2D[] hits = Physics2D.GetRayIntersectionAll(ray, Mathf.Infinity);

        Debug.Log($"Луч выпущен. Найдено объектов под курсором: {hits.Length}");

        foreach (var hit in hits)
        {
            string layerName = LayerMask.LayerToName(hit.collider.gameObject.layer);
            Debug.Log($"> Попали в: '{hit.collider.name}' | Слой: {layerName} | Есть Building компонент: {hit.collider.GetComponent<Building>() != null}");
        }

        // 4. Оригинальная логика
        RaycastHit2D target = Physics2D.GetRayIntersection(ray, Mathf.Infinity, _buildingLayer);

        if (target.collider != null)
        {
            Debug.Log(">>> УСПЕХ: Оригинальный рейкаст нашел цель!");
            Building building = target.collider.GetComponent<Building>();
            if (building != null)
            {
                SelectBuilding(building);
                return;
            }
            else
            {
                Debug.LogWarning(">>> Но на объекте нет скрипта Building!");
            }
        }
        else
        {
            Debug.Log($">>> ПРОВАЛ: Оригинальный рейкаст с маской {_buildingLayer.value} ничего не нашел.");
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