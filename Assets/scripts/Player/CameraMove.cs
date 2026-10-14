using UnityEngine;
using Mirror;

public class CameraMove : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _panSpeed = 20f;         // Базовая скорость движения
    [SerializeField] private float _zoomSpeed = 5f;         // Скорость зума
    [SerializeField] private Vector2 _zoomRange = new(5, 15); // Min/Max зум

    [Header("Границы мира")]
    [Tooltip("Отступ от края мира, чтобы камера не упиралась в границу.")]
    [SerializeField] private float _panMargin = 5f;

    // Границы заполняются, когда мир уже создан: в инициализаторах полей
    // к ядру обращаться нельзя, оно ещё не готово (перенос исправления камеры
    // из ветки features/12_minimap, коммит fae3a3c).
    private Vector2 _panLimitX;
    private Vector2 _panLimitY;
    private bool _limitsReady;

    private Camera _mainCamera;
    private Vector3 _dragOrigin;

    void Start()
    {
        _mainCamera = Camera.main;
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        UpdatePanLimits();

        HandleKeyboardMovement();
        HandleMousePan();
        HandleMouseZoom();
        MoveCamera();
    }

    /// <summary>
    /// Берёт границы перемещения из размеров мира. Пока карты нет — двигаемся без ограничений.
    /// </summary>
    private void UpdatePanLimits()
    {
        if (_limitsReady) return;

        var kernel = GameKernel.Instance;
        if (kernel == null || !kernel.TryGetSystem(out WorldSystem worldSystem) || worldSystem.WorldMap == null) return;

        Map map = worldSystem.WorldMap;
        _panLimitX = new Vector2(_panMargin, Mathf.Max(_panMargin, map.Width - _panMargin));
        _panLimitY = new Vector2(_panMargin, Mathf.Max(_panMargin, map.Height - _panMargin));
        _limitsReady = true;
    }

    private void ClampCameraPosition()
    {
        if (!_limitsReady) return;

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, _panLimitX.x, _panLimitX.y);
        position.y = Mathf.Clamp(position.y, _panLimitY.x, _panLimitY.y);
        transform.position = position;
    }

    private void MoveCamera()
    {
        _mainCamera.transform.localPosition = new Vector3(transform.position.x, transform.position.y, -1f);
        transform.position = Vector2.MoveTowards(transform.position, _mainCamera.transform.localPosition, Time.deltaTime);
    }

    private void HandleKeyboardMovement()
    {
        Vector3 moveDir = new Vector3(
            Input.GetAxis("Horizontal"),
            Input.GetAxis("Vertical"),
            0
        );

        transform.position += _panSpeed * Time.deltaTime * moveDir;
        ClampCameraPosition();
    }

    private void HandleMousePan()
    {
        // Драг камеры ПКМ
        if (Input.GetMouseButtonDown(1))
        {
            _dragOrigin = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 difference = _dragOrigin - _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            transform.position += difference;
            ClampCameraPosition();
        }
    }

    private void HandleMouseZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        _mainCamera.orthographicSize = Mathf.Clamp(
            _mainCamera.orthographicSize - scroll * _zoomSpeed,
            _zoomRange.x,
            _zoomRange.y
        );
    }
}
