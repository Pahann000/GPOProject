using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Пользовательский интерфейс для выбора и строительства зданий
/// </summary>
public class BuildingUI : MonoBehaviour
{
    [Header("Building Settings")]
    [SerializeField] private BuildingData[] _availableBuildings; // Доступные для строительства здания
    [SerializeField] private Button _buildingButtonPrefab;      // Префаб кнопки здания
    [SerializeField] private Transform _buttonsContainer;       // Контейнер для кнопок

    private BuildingSystem _buildingSystem; // Ссылка на систему строительства

    private void Start()
    {
        // Получаем ссылку на систему строительства
        _buildingSystem = FindObjectOfType<BuildingSystem>();

        // Создаем кнопки для всех доступных зданий
        CreateBuildingButtons();
    }

    /// <summary>
    /// Создает кнопки для каждого доступного здания
    /// </summary>
    private void CreateBuildingButtons()
    {
        foreach (var building in _availableBuildings)
        {
            // Создаем новую кнопку
            var button = Instantiate(_buildingButtonPrefab, _buttonsContainer);

            // Устанавливаем иконку здания
            button.GetComponent<Image>().sprite = building.Icon;

            // Добавляем обработчик клика
            button.onClick.AddListener(() => _buildingSystem.StartBuildingPlacement(building));

            // Можно добавить Tooltip с информацией о здании
            // button.GetComponent<TooltipTrigger>().SetTooltip(building.DisplayName, building.Description);
        }
    }
}

