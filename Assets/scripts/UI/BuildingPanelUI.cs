using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

public class BuildingPanelUI : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private GameObject buildingButtonPrefab;
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;

    [Header("Доступные здания")]
    [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();

    private BuilderSystem _builderSystem;
    private bool _isPanelOpen = false;

    private Dictionary<BuildingData, Button> _buildingButtons = new Dictionary<BuildingData, Button>();

    /// <summary>
    /// BuilderSystem берётся лениво: он регистрируется в своём Start(), а порядок
    /// вызова Start у объектов недетерминирован — раньше ссылка навсегда оставалась null.
    /// </summary>
    private BuilderSystem Builder
    {
        get
        {
            if (_builderSystem == null)
            {
                var kernel = GameKernel.Instance;
                if (kernel != null && kernel.TryGetSystem(out BuilderSystem builder))
                {
                    _builderSystem = builder;
                }
            }

            return _builderSystem;
        }
    }

    void Start()
    {
        var kernel = GameKernel.Instance;
        if (kernel != null)
        {
            kernel.EventBus.Subscribe<ResourceChangedEvent>(OnResourceChangedBus);
        }

        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);
        
        InitializeButtons();
        
        // Закрываем панель по умолчанию
        if (panel != null)
            panel.SetActive(false);
    }
    
    void InitializeButtons()
    {
        if (buttonsContainer == null || buildingButtonPrefab == null)
        {
            Debug.LogError("Не назначен buttonsContainer или buildingButtonPrefab!");
            return;
        }
        
        // Очищаем старые кнопки
        foreach (Transform child in buttonsContainer)
            Destroy(child.gameObject);
        
        // Создаем новые кнопки
        foreach (BuildingData buildingData in availableBuildings)
        {
            if (buildingData == null) continue;
            
            CreateButtonForBuilding(buildingData);
        }
    }
    
    void CreateButtonForBuilding(BuildingData buildingData)
    {
        // Создаем кнопку
        GameObject buttonGO = Instantiate(buildingButtonPrefab, buttonsContainer);
        
        // Инициализируем контроллер кнопки
        BuildingButtonUI buttonUI = buttonGO.GetComponent<BuildingButtonUI>();
        if (buttonUI != null)
        {
            buttonUI.Initialize(buildingData, this);
            _buildingButtons[buildingData] = buttonUI.GetComponent<Button>();
        }
    }
    
    void TogglePanel()
    {
        _isPanelOpen = !_isPanelOpen;
        
        if (panel != null)
            panel.SetActive(_isPanelOpen);
    }
    
    private void UpdateButtonAvailability(BuildingData data, Button button)
    {
        if (button == null || data == null) return;

        // Проверяем, есть ли стоимость строительства
        bool hasCost = data.ConstructionCost != null && data.ConstructionCost.Length > 0;

        if (hasCost)
        {
            Player player = null;
            var kernel = GameKernel.Instance;
            if (kernel != null && kernel.TryGetSystem(out UISystem uiSystem))
            {
                player = uiSystem.LocalPlayer;
            }

            // Раньше здесь был GetSystem<UISystem>().LocalPlayer без проверок — NRE.
            bool canAfford = player != null && player.Resources.HasResources(data.ConstructionCost);
            button.interactable = canAfford;

            // Визуальная индикация
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = canAfford ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }
        }
        else
        {
            // Если здание бесплатное, кнопка всегда активна
            button.interactable = true;
        }
    }

    private void OnResourceChangedBus(ResourceChangedEvent evt)
    {
        // Обновляем доступность всех кнопок при изменении ресурсов
        foreach (var kvp in _buildingButtons)
        {
            UpdateButtonAvailability(kvp.Key, kvp.Value);
        }
    }

    private void UpdateToggleButtonText()
    {
        if (toggleButtonText != null)
            toggleButtonText.text = _isPanelOpen ? "❌ Закрыть" : "🏗️ Строительство";
    }

    private void OnDestroy()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChangedBus);
        }
    }

    // Публичные методы
    public void AddBuilding(BuildingData buildingData)
    {
        if (!availableBuildings.Contains(buildingData))
        {
            availableBuildings.Add(buildingData);
            CreateButtonForBuilding(buildingData);
        }
    }

    public void RemoveBuilding(BuildingData buildingData)
    {
        if (availableBuildings.Contains(buildingData))
        {
            availableBuildings.Remove(buildingData);

            if (_buildingButtons.ContainsKey(buildingData))
            {
                Destroy(_buildingButtons[buildingData].gameObject);
                _buildingButtons.Remove(buildingData);
            }
        }
    }

    public void SetBuildingsVisible(bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);
        _isPanelOpen = visible;
        UpdateToggleButtonText();
    }
}