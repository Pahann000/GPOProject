using UnityEngine;

/// <summary>
/// Класс, реализующий движение камеры игрока.
/// </summary>
public class CameraMove : MonoBehaviour
{
    /// <summary>
    /// Скорость сглаживания.
    /// </summary>
    public float smoothSpeed = 0.125f;

    /// <summary>
    /// Смещение камеры относительно игрока.
    /// </summary>
    public Vector3 offset;

    /// <summary>
    /// Каждый кадр проверяет нажатые кнопки и двигает камеру игрока. 
    /// </summary>
    void LateUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");

        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, moveVertical, 0.0f);

        // Желаемая позиция камеры с учетом смещения 
        Vector3 desiredPosition = Camera.main.transform.position + offset + movement;

        // Интерполяция позиции камеры для плавного следования 
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Обновление позиции камеры 
        transform.position = smoothedPosition;
    }
}
