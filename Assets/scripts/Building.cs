using UnityEngine;

/// <summary>
/// ��������� ��������� � � ���������. 
/// </summary>
public class Building : MonoBehaviour
{
    [Header("Building Settings")]
    public BuildingType buildingType;

    private float _lastProductionTime;

    void FixedUpdate()
    {
        if (buildingType.ProducesResource && Time.time - _lastProductionTime > buildingType.ProductionCooldown)
        {
            ProduceResource();
            _lastProductionTime = Time.time;
        }
    }

    /// <summary>
    /// ������� ������������ ��������.
    /// </summary>
    private void ProduceResource()
    {
        // ������ ������������ ��������
        
    }
}