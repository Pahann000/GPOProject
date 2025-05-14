using System.Text;
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

    private void CreateBuildingButtons()
    {
        foreach (var data in _availableBuildings)
        {
            var btn = Instantiate(_buildingButtonPrefab, _buttonsContainer);
            btn.image.sprite = data.Icon;

            // Получаем компонент Tooltip
            var tooltip = btn.GetComponent<Tooltip>();
            if (tooltip == null)
            {
                Debug.LogError("Tooltip component missing on button prefab!");
                continue;
            }

            // Формируем текст
            var sb = new StringBuilder();
            sb.AppendLine(data.DisplayName);
            sb.AppendLine("Cost:");
            foreach (var res in data.ConstructionCost.Resources)
            {
                sb.AppendLine($"{res.Key}: {res.Value}");
            }

            tooltip.SetText(sb.ToString());

            btn.onClick.AddListener(() => _buildingSystem.StartBuildingPlacement(data));
        }
    }
}

