using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Управляет выделением зданий (и в будущем, возможно, юнитов) на карте.
/// Рассылает события BuildingSelectedEvent и BuildingDeselectedEvent.
/// </summary>
public class SelectionSystem : IGameSystem
{
    public string SystemName => "Selection System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private Camera _mainCamera;

    // Слой, на котором физически находятся все построенные здания
    private LayerMask _buildingLayer;

    // Ссылка на текущее выбранное здание
    private Building _selectedBuilding;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        _mainCamera = Camera.main;

        _buildingLayer = LayerMask.GetMask("Building");

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        // Если игрок сейчас строит здание, мы блокируем возможность выделять старые здания,
        // чтобы избежать двойного клика (поставил здание и сразу выделил соседнее).
        var builder = _kernel.GetSystem<BuilderSystem>();
        if (builder != null && builder.IsPlacingBuilding()) return;

        // Отмена выделения по нажатию Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Deselect();
            return;
        }

        // Выделение объекта по левому клику мыши
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

    /// <summary>
    /// Обрабатывает клик мыши: пускает луч и ищет здание.
    /// </summary>
    private void HandleClick()
    {
        // Предотвращаем "пробивание" клика сквозь элементы интерфейса (панели, кнопки)
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        // Пускаем луч из камеры в точку курсора
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, _buildingLayer);

        if (hit.collider != null)
        {
            // Используем GetComponentInParent, так как коллайдер может висеть на дочернем объекте префаба
            Building building = hit.collider.GetComponentInParent<Building>();
            if (building != null)
            {
                SelectBuilding(building);
                return; // Успешно выделили, выходим
            }
        }

        // Если луч никуда не попал (кликнули по земле) — снимаем текущее выделение
        Deselect();
    }

    /// <summary>
    /// Выделяет указанное здание и сообщает об этом через Шину Событий.
    /// </summary>
    private void SelectBuilding(Building building)
    {
        if (_selectedBuilding == building) return; // Объект уже выделен

        // Снимаем визуальное выделение с предыдущего объекта
        if (_selectedBuilding != null)
        {
            _selectedBuilding.Deselect();
        }

        _selectedBuilding = building;
        _selectedBuilding.Select(); // Включает визуальный кружок/рамку на объекте

        // Рассылаем событие, чтобы SelectionPanelUI показала информацию
        _kernel.EventBus.Raise(new BuildingSelectedEvent(_selectedBuilding));
    }

    /// <summary>
    /// Снимает выделение с текущего объекта и сообщает об этом интерфейсу.
    /// </summary>
    public void Deselect()
    {
        if (_selectedBuilding != null)
        {
            _selectedBuilding.Deselect();
            _selectedBuilding = null;

            // Рассылаем событие, чтобы UI скрыл панель информации
            _kernel.EventBus.Raise(new BuildingDeselectedEvent());
        }
    }
}