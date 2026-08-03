using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


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
        ProduceBuilding produceBuilding = selectedBuilding as ProduceBuilding;
        if (produceBuilding != null && productionInfoPanel != null)
        {
            productionInfoPanel.SetActive(true);
            UpdateProductionInfo(produceBuilding);
        }
        else if (productionInfoPanel != null)
        {
            productionInfoPanel.SetActive(false);
        }

        // Обновляем доступность кнопок
        UpdateButtonsAvailability();
    }

    private void UpdateProductionInfo(ProduceBuilding production)
    {
        if (production == null || production.ProduceData == null) return;

        // Передаем напрямую ResourceBundle без конвертаций
        if (inputResourcesText != null)
        {
            inputResourcesText.text = FormatResources(production.ProduceData.InputResources, "Вход:");
        }

        if (outputResourcesText != null)
        {
            outputResourcesText.text = FormatResources(production.ProduceData.OutputResources, "Выход:");
        }
    }

    private string FormatResources(ResourceBundle bundle, string prefix)
    {
        if (bundle == null || bundle.Resources == null || bundle.Resources.Count == 0)
            return $"{prefix} Нет";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine(prefix);

        // Перебираем пары "Тип ресурса -> Количество" прямо из словаря ResourceBundle
        foreach (var resource in bundle.Resources)
        {
            sb.AppendLine($"  {resource.Key}: {resource.Value}");
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
        if (selectedBuilding == null || selectedBuilding.Data == null) return;

        Player owner = selectedBuilding.Data.Owner != null
            ? selectedBuilding.Data.Owner
            : (Mirror.NetworkClient.localPlayer != null ? Mirror.NetworkClient.localPlayer.GetComponent<Player>() : null);

        if (owner == null) return;

        ResourcePair[] upgradeCost = CalculateUpgradeCost(selectedBuilding);

        if (GameKernel.Instance.GetSystem<ResourceSystem>().TrySpendResources(owner, upgradeCost))
        {
            Debug.Log($"Улучшаем здание: {selectedBuilding.Data.DisplayName}");
            // selectedBuilding.Upgrade();
        }
    }

    private void RepairBuilding()
    {
        if (selectedBuilding == null || selectedBuilding.Data == null) return;

        Player owner = selectedBuilding.Data.Owner != null
            ? selectedBuilding.Data.Owner
            : (Mirror.NetworkClient.localPlayer != null ? Mirror.NetworkClient.localPlayer.GetComponent<Player>() : null);

        if (owner == null) return;

        int repairCost = CalculateRepairCost(selectedBuilding);
        ResourcePair[] costBundle = new ResourcePair[] { new ResourcePair(ResourceType.Minerals, repairCost) };

        if (GameKernel.Instance.GetSystem<ResourceSystem>().TrySpendResources(owner, costBundle))
        {
            selectedBuilding.CurrentHealth = selectedBuilding.Data.MaxHealth;
            Debug.Log($"Здание {selectedBuilding.Data.DisplayName} отремонтировано");
        }
    }

    private void DemolishBuilding()
    {
        if (selectedBuilding == null || selectedBuilding.Data == null) return;

        // Безопасно определяем владельца здания (или локального игрока)
        Player owner = selectedBuilding.Data.Owner != null
            ? selectedBuilding.Data.Owner
            : (Mirror.NetworkClient.localPlayer != null ? Mirror.NetworkClient.localPlayer.GetComponent<Player>() : null);

        // Расчитываем 50% возврат ресурсов
        ResourcePair[] refund = CalculateRefund(selectedBuilding.Data.ConstructionCost);

        if (owner != null && refund.Length > 0)
        {
            GameKernel.Instance.GetSystem<ResourceSystem>().AddResources(owner, refund);
        }

        Destroy(selectedBuilding.gameObject);
        Deselect();

        Debug.Log($"Здание снесено. Возвращено ресурсов на: {CalculateRefundValue(refund)}");
    }

    private ResourcePair[] CalculateUpgradeCost(Building building)
    {
        if (building == null || building.Data == null) return new ResourcePair[0];
        return MultiplyResourceBundle(building.Data.ConstructionCost, 0.5f);
    }

    private int CalculateRepairCost(Building building)
    {
        int missingHealth = building.Data.MaxHealth - building.CurrentHealth;
        return missingHealth * 2; // Примерная формула: 2 единицы ресурса за 1 HP
    }

    private ResourcePair[] CalculateRefund(ResourceBundle originalCost)
    {
        return MultiplyResourceBundle(originalCost, 0.5f);
    }

    private int CalculateRefundValue(ResourcePair[] refund)
    {
        if (refund == null) return 0;
        int total = 0;
        foreach (var resource in refund)
        {
            total += resource.Amount;
        }
        return total;
    }

    private ResourcePair[] MultiplyResourceBundle(ResourceBundle bundle, float multiplier)
    {
        if (bundle == null || bundle.Resources == null) return new ResourcePair[0];

        List<ResourcePair> result = new List<ResourcePair>();
        foreach (var kvp in bundle.Resources)
        {
            result.Add(new ResourcePair(kvp.Key, Mathf.RoundToInt(kvp.Value * multiplier)));
        }
        return result.ToArray();
    }

    //private IEnumerator TestPanel()
    //{
    //    yield return new WaitForSeconds(1f);

    //    // Создайте тестовое здание для проверки
    //    Debug.Log("Testing Selection Panel...");

    //    // Здесь можно временно заполнить панель тестовыми данными
    //    if (nameText != null) nameText.text = "Тестовая шахта";
    //    if (healthText != null) healthText.text = "Здоровье: 75/100";
    //    if (stateText != null) stateText.text = "Состояние: Работает";
    //    if (healthBar != null) healthBar.value = 0.75f;

    //    if (panel != null)
    //        panel.SetActive(true);
    //}
}