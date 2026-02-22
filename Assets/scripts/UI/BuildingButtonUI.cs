using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private BuildingData _buildingData;
    private BuildingPanelUI _parentPanel;

    /// <summary>
    /// Инициализирует кнопку с данными здания
    /// </summary>
    public void Initialize(BuildingData data, BuildingPanelUI parentPanel)
    {
        _buildingData = data;
        _parentPanel = parentPanel;

        if (button == null) button = GetComponent<Button>();

        // Устанавливаем визуальные элементы
        if (iconImage != null && data.Icon != null) iconImage.sprite = data.Icon;

        if (nameText != null) nameText.text = data.DisplayName;

        SetupTooltipAndCost(data);
        
        // Клик -> обращаемся к Ядру
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClicked);

        var hoverHandler = gameObject.AddComponent<BuildingButtonHover>();
        hoverHandler.Initialize(button, tooltipObject, backgroundImage, hoverColor);

        // Обновляем доступность
        UpdateAvailability();
    }

    private void SetupTooltipAndCost(BuildingData data)
    {
        string costString = "";
        if (data.ConstructionCost.Resources != null)
        {
            foreach (var res in data.ConstructionCost.Resources)
            {
                costString += $"{res.Type}: {res.Amount}";
            }
        }

        if (costText != null) costText.text = string.IsNullOrEmpty(costString) ? "Бесплатно" : costString;

        if (tooltipText != null && tooltipObject != null)
        {
            tooltipText.text = $"<b>{data.DisplayName}</b>\nРазмер: {data.Width}x{data.Height}";
            tooltipObject.SetActive(false);
        }
    }

    private void OnButtonClicked()
    {
        if (_buildingData == null || GameKernel.Instance == null) return;

        var builer = GameKernel.Instance.GetSystem<BuilderSystem>();
        if (builer != null)
        {
            builer.StartPlacement(_buildingData);
            _parentPanel.SetBuildingsVisible(false);
        }
    }

    /// <summary>
    /// Обновляет доступность кнопки в зависимости от ресурсов
    /// </summary>
    public void UpdateAvailability()
    {
        if (_buildingData == null || GameKernel.Instance == null) return;

        var resourceSys = GameKernel.Instance.GetSystem<ResourceSystem>();
        bool canAfford = resourceSys != null && resourceSys.HasResources(_buildingData.ConstructionCost);

        // Визуальная индикация доступности
        if (iconImage != null) iconImage.color = canAfford ? affordableColor : unaffordableColor;

        if (button != null) button.interactable = canAfford;

        // TODO: Можно добавить дополнительные эффекты для недоступных зданий
        //if (!canAfford)
        //{
        //    // Например, добавить затемнение
        //    if (backgroundImage != null)
        //        backgroundImage.color = new Color(0.3f, 0.3f, 0.3f, 0.7f);
        //}
    }
}