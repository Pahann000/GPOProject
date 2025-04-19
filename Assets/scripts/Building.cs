using UnityEngine;

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

    private void ProduceResource()
    {
        // Логика производства ресурсов
        
    }
}