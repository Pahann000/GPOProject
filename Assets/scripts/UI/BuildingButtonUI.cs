using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class BuildingButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Color Settings")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color unaffordableColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    [SerializeField] private Color hoverColor = new Color(0.8f, 0.8f, 1f, 1f);

    [Header("Tooltip Settings")]
    [SerializeField] private GameObject tooltipObject;
    [SerializeField] private TextMeshProUGUI tooltipText;

    private BuildingData buildingData;
    private System.Action onClickCallback;

    /// <summary>
    /// Инициализирует кнопку с данными здания
    /// </summary>
    public void Initialize(BuildingData data, System.Action onClick)
    {
        buildingData = data;
        onClickCallback = onClick;

        // Устанавливаем визуальные элементы
        if (iconImage != null && data.Icon != null)
            iconImage.sprite = data.Icon;

        if (nameText != null)
            nameText.text = data.DisplayName;

        if (costText != null)
            costText.text = FormatCost(data.ConstructionCost);

        // Назначаем обработчик клика
        if (button != null)
            button.onClick.AddListener(OnButtonClick);

        // Настраиваем тултип
        SetupTooltip(data);

        // Обновляем доступность
        UpdateAvailability();

        // Добавляем обработчики наведения мыши
        AddHoverEffects();
    }

    private string FormatCost(ResourceBundle cost)
    {
        if (cost.Resources == null || cost.Resources.Count == 0)
            return "Бесплатно";

        StringBuilder sb = new StringBuilder();
        foreach (var resource in cost.Resources)
        {
            sb.AppendLine($"{resource.Type}: {resource.Amount}");
        }
        return sb.ToString();
    }

    private void SetupTooltip(BuildingData data)
    {
        if (tooltipText == null || tooltipObject == null) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"<b>{data.DisplayName}</b>");
        sb.AppendLine();

        // Стоимость строительства
        sb.AppendLine("<color=yellow>Стоимость:</color>");
        if (data.ConstructionCost.Resources != null)
        {
            foreach (var resource in data.ConstructionCost.Resources)
            {
                sb.AppendLine($"{resource.Type}: {resource.Amount}");
            }
        }
        sb.AppendLine();

        // Размер здания
        sb.AppendLine($"<color=yellow>Размер:</color> {data.Width}x{data.Height}");

        // Производство (если есть)
        if (data.OutputResources.Resources != null && data.OutputResources.Resources.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<color=yellow>Производит:</color>");
            foreach (var output in data.OutputResources.Resources)
            {
                sb.AppendLine($"{output.Type}: {output.Amount}");
            }
        }

        tooltipText.text = sb.ToString();
        tooltipObject.SetActive(false); // Скрываем по умолчанию
    }

    private void AddHoverEffects()
    {
        // Добавляем компоненты для обработки наведения
        var hoverHandler = gameObject.AddComponent<BuildingButtonHover>();
        hoverHandler.Initialize(button, tooltipObject, backgroundImage, hoverColor);
    }

    private void OnButtonClick()
    {
        onClickCallback?.Invoke();
    }

    /// <summary>
    /// Обновляет доступность кнопки в зависимости от ресурсов
    /// </summary>
    public void UpdateAvailability()
    {
        if (buildingData == null || ResourceManager.Instance == null) return;

        bool canAfford = ResourceManager.Instance.HasResources(buildingData.ConstructionCost);

        // Визуальная индикация доступности
        if (iconImage != null)
            iconImage.color = canAfford ? affordableColor : unaffordableColor;

        if (button != null)
            button.interactable = canAfford;

        // Можно добавить дополнительные эффекты для недоступных зданий
        if (!canAfford)
        {
            // Например, добавить затемнение
            if (backgroundImage != null)
                backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        }
    }

    private void Update()
    {
        // Динамически обновляем доступность (можно делать реже для оптимизации)
        UpdateAvailability();
    }

    /// <summary>
    /// Получает данные здания этой кнопки
    /// </summary>
    public BuildingData GetBuildingData()
    {
        return buildingData;
    }

    /// <summary>
    /// Возвращает компонент кнопки
    /// </summary>
    public Button GetButton()
    {
        return button;
    }
}