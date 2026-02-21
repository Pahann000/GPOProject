using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
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

        // Основная информация
        if (nameText != null)
            nameText.text = selectedBuilding.Data.DisplayName;

        if (healthText != null)
            healthText.text = $"Здоровье: {selectedBuilding.CurrentHealth}/{selectedBuilding.Data.MaxHealth}";

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
            inputResourcesText.text = FormatResources(production.inputResources, "Вход:");

        if (outputResourcesText != null)
            outputResourcesText.text = FormatResources(production.outputResources, "Выход:");

        // Прогресс производства (примерная реализация)
        if (productionProgress != null)
        {
            // Здесь нужно добавить логику расчета прогресса
            // productionProgress.value = CalculateProductionProgress(production);
        }
    }

    private string FormatResources(ResourceBundle resources, string prefix)
    {
        if (resources.Resources == null || resources.Resources.Count == 0)
            return $"{prefix} Нет";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(prefix);

        foreach (var resource in resources.Resources)
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

        // Здесь реализуйте логику улучшения здания
        Debug.Log($"Улучшаем здание: {selectedBuilding.Data.DisplayName}");

        // Пример: проверка ресурсов и улучшение
        ResourceBundle upgradeCost = CalculateUpgradeCost(selectedBuilding);
        if (GameKernel.Instance.GetSystem<ResourceSystem>().TrySpendResources(upgradeCost))
        {
            // Применить улучшения к зданию
            // selectedBuilding.Upgrade();
        }
    }

    private void RepairBuilding()
    {
        if (selectedBuilding == null) return;

        int repairCost = CalculateRepairCost(selectedBuilding);
        ResourceBundle costBundle = CreateResourceBundle(ResourceType.Minerals, repairCost);

        if (GameKernel.Instance.GetSystem<ResourceSystem>().TrySpendResources(costBundle))
        {
            selectedBuilding.CurrentHealth = selectedBuilding.Data.MaxHealth;
            Debug.Log($"Здание {selectedBuilding.Data.DisplayName} отремонтировано");
        }
    }

    private void DemolishBuilding()
    {
        if (selectedBuilding == null) return;

        // Возвращаем часть ресурсов при сносе
        ResourceBundle refund = CalculateRefund(selectedBuilding.Data.ConstructionCost);
        GameKernel.Instance.GetSystem<ResourceSystem>().AddResources(refund);

        // Уничтожаем здание
        Destroy(selectedBuilding.gameObject);
        Deselect();

        Debug.Log($"Здание снесено. Возвращено ресурсов на: {CalculateRefundValue(refund)}");
    }

    private ResourceBundle CalculateUpgradeCost(Building building)
    {
        // Пример: стоимость улучшения = 50% от исходной стоимости
        return MultiplyResourceBundle(building.Data.ConstructionCost, 0.5f);
    }

    private int CalculateRepairCost(Building building)
    {
        int missingHealth = building.Data.MaxHealth - building.CurrentHealth;
        return missingHealth * 2; // Примерная формула: 2 единицы ресурса за 1 HP
    }

    private ResourceBundle CalculateRefund(ResourceBundle originalCost)
    {
        // Возвращаем 50% от исходной стоимости
        return MultiplyResourceBundle(originalCost, 0.5f);
    }

    private int CalculateRefundValue(ResourceBundle refund)
    {
        int total = 0;
        foreach (var resource in refund.Resources)
        {
            total += resource.Amount;
        }
        return total;
    }

    private ResourceBundle MultiplyResourceBundle(ResourceBundle bundle, float multiplier)
    {
        var result = new ResourceBundle();
        result.Resources = new System.Collections.Generic.List<ResourceBundle.ResourcePair>();

        foreach (var resource in bundle.Resources)
        {
            result.Resources.Add(new ResourceBundle.ResourcePair
            {
                Type = resource.Type,
                Amount = Mathf.RoundToInt(resource.Amount * multiplier)
            });
        }

        return result;
    }

    private ResourceBundle CreateResourceBundle(ResourceType type, int amount)
    {
        var bundle = new ResourceBundle();
        bundle.Resources = new System.Collections.Generic.List<ResourceBundle.ResourcePair>
        {
            new ResourceBundle.ResourcePair { Type = type, Amount = amount }
        };
        return bundle;
    }

    private IEnumerator TestPanel()
    {
        yield return new WaitForSeconds(1f);

        // Создайте тестовое здание для проверки
        Debug.Log("Testing Selection Panel...");

        // Здесь можно временно заполнить панель тестовыми данными
        if (nameText != null) nameText.text = "Тестовая шахта";
        if (healthText != null) healthText.text = "Здоровье: 75/100";
        if (stateText != null) stateText.text = "Состояние: Работает";
        if (healthBar != null) healthBar.value = 0.75f;

        if (panel != null)
            panel.SetActive(true);
    }
}