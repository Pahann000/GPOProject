using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingButtonController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;

    private BuildingData _buildingData;
    private Button _button;

    void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
            _button = gameObject.AddComponent<Button>();
    }

    public void Initialize(BuildingData buildingData)
    {
        _buildingData = buildingData;

        // Настройка визуала
        if (iconImage != null && buildingData.Icon != null)
            iconImage.sprite = buildingData.Icon;

        if (nameText != null)
            nameText.text = buildingData.DisplayName;

        if (costText != null)
        {
            // Форматирование стоимости
            string costString = "";
            foreach (var resource in buildingData.ConstructionCost.Resources)
            {
                costString += $"{resource.Type}: {resource.Amount}\n";
            }
            costText.text = costString;
        }

        // Назначаем обработчик клика
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnButtonClicked);

        Debug.Log($"Кнопка инициализирована: {buildingData.DisplayName}");
    }

    private void OnButtonClicked()
    {
        Debug.Log($"Нажата кнопка: {_buildingData.DisplayName}");

        if (_buildingData == null) return;

        if (GameKernel.Instance != null)
        {
            var builder = GameKernel.Instance.GetSystem<BuilderSystem>();
            if (builder != null)
            {
                builder.StartPlacement(_buildingData);
            }
            else
            {
                Debug.LogError("BuilderSystem не найдена в Ядре!");
            }
        }
        else
        {
            Debug.LogError("GameKernel не найдена!");
        }

        // Закрываем панель после выбора
        BuildingPanelUI panelUI = FindFirstObjectByType<BuildingPanelUI>();
        if (panelUI != null)
        {
            panelUI.SetBuildingsVisible(false);
        }
    }

    // Обновляем доступность кнопки
    public void UpdateAvailability(bool canAfford)
    {
        if (_button != null)
        {
            _button.interactable = canAfford;

            // Визуальная индикация
            if (iconImage != null)
                iconImage.color = canAfford ? Color.white : Color.gray;
        }
    }
}