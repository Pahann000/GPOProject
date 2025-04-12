using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public float smoothSpeed = 0.125f; // �������� ����������� 
    public Vector3 offset; // �������� ������ ������������ ������ 

    void LateUpdate()
    {

        float moveHorizontal = Input.GetAxis("Horizontal");

        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, moveVertical, 0.0f);

        // �������� ������� ������ � ������ �������� 
        Vector3 desiredPosition = Camera.main.transform.position + offset + movement;

        // ������������ ������� ������ ��� �������� ���������� 
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // ���������� ������� ������ 
        transform.position = smoothedPosition;
    }
}
