using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;


public class SelectionPanelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button repairButton;
    [SerializeField] private Button demolishButton;

    [Header("Production Info")]
    [SerializeField] private GameObject productionInfoPanel;
    [SerializeField] private TextMeshProUGUI inputResourcesText;
    [SerializeField] private TextMeshProUGUI outputResourcesText;
    [SerializeField] private Slider productionProgress;

    [Header("Colors")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color damagedColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private Color operationalColor = new Color(0.56f, 0.93f, 0.56f);

    private Building selectedBuilding;

    private void Start()
    {
        // Назначаем обработчики кнопок
        if (upgradeButton != null) upgradeButton.onClick.AddListener(UpgradeBuilding);

        if (repairButton != null) repairButton.onClick.AddListener(RepairBuilding);

        if (demolishButton != null) demolishButton.onClick.AddListener(DemolishBuilding);

        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Subscribe<BuildingSelectedEvent>(OnBuildableSelected);
            GameKernel.Instance.EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildableDeselected);
        }

        if (panel != null) panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildableSelected);
            GameKernel.Instance.EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildableDeselected);
        }
    }

    private void OnBuildableSelected(BuildingSelectedEvent evt)
    {
        selectedBuilding = evt.SelectedBuilding;

        if (panel != null) panel.SetActive(true);

        UpdateUI();
    }

    private void OnBuildableDeselected(BuildingDeselectedEvent evt)
    {
        selectedBuilding = null;

        if (panel != null) panel.SetActive(false);
    }

    public void SelectBuilding(Building building)
    {
        selectedBuilding = building;

        if (panel != null)
            panel.SetActive(true);

        UpdateUI();
    }

    public void Deselect()
    {
        selectedBuilding = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void Update()
    {
        if (selectedBuilding != null)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (selectedBuilding == null) return;

        // Без данных нет ни имени, ни максимального здоровья: раньше обращение
        // к Data напрямую падало с NullReferenceException на недонастроенном префабе.
        BuildingData data = selectedBuilding.Data;
        if (data == null) return;

        // Основная информация
        if (nameText != null)
        {
            string factionName = FactionName(selectedBuilding.Faction);
            nameText.text = string.IsNullOrEmpty(factionName)
                ? data.DisplayName
                : $"{data.DisplayName} ({factionName})";
        }

        if (healthText != null)
            healthText.text = $"Здоровье: {selectedBuilding.CurrentHealth}/{data.MaxHealth}";

        if (stateText != null)
            stateText.text = $"Состояние: {GetStateText(selectedBuilding.State)}";

        if (healthBar != null)
        {
            healthBar.value = (float)selectedBuilding.CurrentHealth / selectedBuilding.Data.MaxHealth;

            // Цвет полосы здоровья в зависимости от состояния
            Image fillImage = healthBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                float healthPercent = (float)selectedBuilding.CurrentHealth / selectedBuilding.Data.MaxHealth;
                if (healthPercent > 0.7f) fillImage.color = healthyColor;
                else if (healthPercent > 0.3f) fillImage.color = damagedColor;
                else fillImage.color = criticalColor;
            }
        }

        // Информация о производстве (для производственных зданий)
        ProductionBuilding productionBuilding = selectedBuilding as ProductionBuilding;
        if (productionBuilding != null && productionInfoPanel != null)
        {
            productionInfoPanel.SetActive(true);
            UpdateProductionInfo(productionBuilding);
        }
        else if (productionInfoPanel != null)
        {
            productionInfoPanel.SetActive(false);
        }

        // Обновляем доступность кнопок
        UpdateButtonsAvailability();
    }

    private void UpdateProductionInfo(ProductionBuilding production)
    {
        if (inputResourcesText != null)
            inputResourcesText.text = FormatResources(production.InputResources, "Вход:");

        if (outputResourcesText != null)
            outputResourcesText.text = FormatResources(production.OutputResources, "Выход:");

        // Прогресс производства (примерная реализация)
        if (productionProgress != null)
        {
            // Здесь нужно добавить логику расчета прогресса
            // productionProgress.value = CalculateProductionProgress(production);
        }
    }

    private string FormatResources(ResourcePair[] resources, string prefix)
    {
        if (resources == null || resources.Length == 0)
            return $"{prefix} Нет";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(prefix);

        foreach (var resource in resources)
        {
            sb.AppendLine($"  {resource.Type}: {resource.Amount}");
        }

        return sb.ToString();
    }

    private void UpdateButtonsAvailability()
    {
        if (selectedBuilding == null) return;

        // Кнопка починки доступна только если здоровье не полное
        if (repairButton != null)
            repairButton.interactable = selectedBuilding.CurrentHealth < selectedBuilding.Data.MaxHealth;

        // Кнопка улучшения (здесь можно добавить дополнительные условия)
        if (upgradeButton != null)
            upgradeButton.interactable = CanUpgradeBuilding(selectedBuilding);

        // Кнопка сноса всегда доступна для выбранного здания
        if (demolishButton != null)
            demolishButton.interactable = true;
    }

    private string GetStateText(BuildingState state)
    {
        switch (state)
        {
            case BuildingState.Planned: return "Запланировано";
            case BuildingState.Constructing: return "Строится";
            case BuildingState.Operational: return "Работает";
            case BuildingState.Damaged: return "Повреждено";
            case BuildingState.Destroyed: return "Уничтожено";
            default: return "Неизвестно";
        }
    }

    private bool CanUpgradeBuilding(Building building)
    {
        // Пример условий для улучшения:
        // 1. Здание должно быть в рабочем состоянии
        // 2. Должны быть доступны ресурсы для улучшения
        // 3. Не должно быть других ограничений
        return building.State == BuildingState.Operational;
    }

    private void UpgradeBuilding()
    {
        if (selectedBuilding == null) return;

        // TODO: реализовать улучшение здания (стоимость и применение эффекта).
        Debug.Log($"[SelectionPanelUI] Улучшение здания {selectedBuilding.Data?.DisplayName} пока не реализовано.");
    }

    private void RepairBuilding()
    {
        if (selectedBuilding == null) return;

        // Стоимость считает и списывает сервер (Building.CmdRepair), клиент только шлёт команду.
        selectedBuilding.CmdRepair();

        Debug.Log($"[SelectionPanelUI] Запрошен ремонт здания {selectedBuilding.Data?.DisplayName}.");
    }

    private void DemolishBuilding()
    {
        if (selectedBuilding == null) return;

        // Сервер проверит владельца и вернёт половину стоимости
        // (см. Building.ReturnResourcesOnDestroy).
        selectedBuilding.CmdDemolish();

        Debug.Log($"[SelectionPanelUI] Запрошен снос здания {selectedBuilding.Data?.DisplayName}.");
    }

    /// <summary>
    /// Название фракции здания для заголовка панели.
    /// </summary>
    private static string FactionName(FactionType faction)
    {
        FactionDefinition definition = FactionAtlas.GetDefinition(faction);
        return definition != null ? definition.DisplayName : string.Empty;
    }
}