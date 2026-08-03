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

    private BaseBuildingData _buildingData;
    private BuildingPanelUI _parentPanel;

    /// <summary>
    /// Инициализирует кнопку с данными здания
    /// </summary>
    public void Initialize(BaseBuildingData data, BuildingPanelUI parentPanel)
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

    private void SetupTooltipAndCost(BaseBuildingData data)
    {
        string costString = "";
        if (data.ConstructionCost != null && data.ConstructionCost.Resources != null)
        {
            foreach (var res in data.ConstructionCost.Resources)
            {
                costString += $"{res.Key}: {res.Value}\n";
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
        if (resourceSys == null) return;

        Player localPlayer = null;
        if (Mirror.NetworkClient.localPlayer != null)
        {
            localPlayer = Mirror.NetworkClient.localPlayer.GetComponent<Player>();
        }

        if (localPlayer == null)
        {
            if (button != null) button.interactable = false;
            return;
        }

        bool canAfford = resourceSys.HasResources(localPlayer, _buildingData.ConstructionCost);

        // Визуальная индикация доступности
        if (iconImage != null) iconImage.color = canAfford ? affordableColor : unaffordableColor;

        if (button != null) button.interactable = canAfford;

        // TODO: Можно добавить дополнительные эффекты для недоступных зданий
        if (iconImage != null) iconImage.color = canAfford ? affordableColor : unaffordableColor;
        if (button != null) button.interactable = canAfford;
    }
}