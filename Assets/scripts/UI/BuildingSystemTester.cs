using UnityEngine;

public class BuildingSystemTester : MonoBehaviour
{
    [Header("Тестовые здания")]
    [SerializeField] private BuildingData testHouse;
    [SerializeField] private BuildingData testFarm;
    [SerializeField] private BuildingData testBarracks;

    [Header("Тестовые ресурсы")]
    [SerializeField] private bool addTestResourcesOnStart = true;

    private BuildingSystem _buildingSystem;
    private ResourceManager _resourceManager;

    void Start()
    {
        _buildingSystem = FindFirstObjectByType<BuildingSystem>();
        _resourceManager = FindFirstObjectByType<ResourceManager>();

        if (addTestResourcesOnStart && _resourceManager != null)
        {
            // Добавляем тестовые ресурсы
            _resourceManager.AddResource(ResourceType.Rock, 1000);
            _resourceManager.AddResource(ResourceType.Minerals, 500);
            _resourceManager.AddResource(ResourceType.Ice, 200);
            _resourceManager.AddResource(ResourceType.Root, 100);
            _resourceManager.AddResource(ResourceType.Energy, 50);
        }

        Debug.Log("=== ТЕСТ СИСТЕМЫ СТРОИТЕЛЬСТВА ===");
        Debug.Log("1. Нажмите B - открыть/закрыть панель строительства");
        Debug.Log("2. Нажмите цифры 1-3 для быстрого строительства");
        Debug.Log("3. Нажмите R - добавить тестовые ресурсы");
        Debug.Log("4. Нажмите C - очистить все здания");
    }

    void Update()
    {
        // Горячие клавиши для тестирования
        if (Input.GetKeyDown(KeyCode.Alpha1) && testHouse != null)
        {
            StartTestBuilding(testHouse);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) && testFarm != null)
        {
            StartTestBuilding(testFarm);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) && testBarracks != null)
        {
            StartTestBuilding(testBarracks);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            AddTestResources();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearAllBuildings();
        }
    }

    private void StartTestBuilding(BuildingData buildingData)
    {
        if (_buildingSystem != null)
        {
            _buildingSystem.StartBuildingPlacement(buildingData);
            Debug.Log($"Начато размещение: {buildingData.DisplayName}");
        }
    }

    private void AddTestResources()
    {
        if (_resourceManager != null)
        {
            _resourceManager.AddResource(ResourceType.Rock, 100);
            _resourceManager.AddResource(ResourceType.Minerals, 50);
            _resourceManager.AddResource(ResourceType.Ice, 20);
            _resourceManager.AddResource(ResourceType.Root, 10);
            _resourceManager.AddResource(ResourceType.Energy, 5);

            Debug.Log("Добавлены тестовые ресурсы");
        }
    }

    private void ClearAllBuildings()
    {
        Building[] buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
        foreach (Building building in buildings)
        {
            Destroy(building.gameObject);
        }
        Debug.Log($"Удалено {buildings.Length} зданий");
    }
}