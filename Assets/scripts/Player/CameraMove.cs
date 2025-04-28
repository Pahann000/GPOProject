using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public float smoothSpeed = 0.125f; // Скорость сглаживания 
    public Vector3 offset; // Смещение камеры относительно игрока 

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
