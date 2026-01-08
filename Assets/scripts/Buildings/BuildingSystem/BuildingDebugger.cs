using UnityEngine;

public class BuildingDebugger : MonoBehaviour
{
    [Header("Режим отладки")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private KeyCode testKey = KeyCode.T;

    [Header("Тестовые данные")]
    [SerializeField] private BuildingData testBuildingData;

    private BuildingSystem _buildingSystem;

    void Start()
    {
        _buildingSystem = GetComponent<BuildingSystem>();
        if (_buildingSystem == null)
            _buildingSystem = FindObjectOfType<BuildingSystem>();

        Debug.Log("=== BuildingDebugger запущен ===");
        Debug.Log($"BuildingSystem найден: {_buildingSystem != null}");
    }

    void Update()
    {
        if (!debugMode) return;

        // Быстрый тест клавишей T
        if (Input.GetKeyDown(testKey) && testBuildingData != null)
        {
            TestBuildingPlacement();
        }

        // Проверка состояния каждый кадр
        if (_buildingSystem != null && _buildingSystem.IsPlacingBuilding())
        {
            Debug.Log($"Режим размещения активен. Превью: {_buildingSystem.GetPreviewPosition()}");
        }
    }

    public void TestBuildingPlacement()
    {
        if (_buildingSystem == null)
        {
            Debug.LogError("BuildingSystem не найден!");
            return;
        }

        if (testBuildingData == null)
        {
            Debug.LogError("TestBuildingData не назначен!");
            return;
        }

        Debug.Log($"=== ТЕСТ СТРОИТЕЛЬСТВА: {testBuildingData.DisplayName} ===");

        // Проверяем префабы
        Debug.Log($"Префаб здания: {testBuildingData.Prefab != null}");
        Debug.Log($"Иконка: {testBuildingData.Icon != null}");

        // Проверяем ресурсы
        ResourceManager resourceManager = FindObjectOfType<ResourceManager>();
        if (resourceManager != null)
        {
            bool hasResources = resourceManager.HasResources(testBuildingData.ConstructionCost);
            Debug.Log($"Достаточно ресурсов: {hasResources}");
        }

        // Запускаем строительство
        _buildingSystem.StartBuildingPlacement(testBuildingData);
        Debug.Log("BuildingPlacement запущен");
    }
}