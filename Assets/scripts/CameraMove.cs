using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _panSpeed = 20f;         // Базовая скорость движения
    [SerializeField] private float _edgePanSpeed = 25f;     // Скорость при движении к краю экрана
    [SerializeField] private float _zoomSpeed = 5f;         // Скорость зума
    [SerializeField] private Vector2 _zoomRange = new(5, 15); // Min/Max зум
    [SerializeField] private Vector2 _panLimitX = new(-50, 50); // Границы по X
    [SerializeField] private Vector2 _panLimitY = new(-50, 50); // Границы по Y

    private Camera _mainCamera;
    private Vector3 _dragOrigin;

    void Start()
    {
        _mainCamera = GetComponent<Camera>();
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleMousePan();
        HandleMouseZoom();
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

    private void ClampCameraPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, _panLimitX.x, _panLimitX.y);
        pos.y = Mathf.Clamp(pos.y, _panLimitY.x, _panLimitY.y);
        transform.position = pos;
    }
}
