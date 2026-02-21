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
    private ResourceSystem _resourceManager;
    private bool _isPanelOpen = false;

    private Dictionary<BuildingData, Button> _buildingButtons = new Dictionary<BuildingData, Button>();

    void Start()
    {
        _builderSystem = GameKernel.Instance.GetSystem<BuilderSystem>();
        _resourceManager = GameKernel.Instance.GetSystem<ResourceSystem>();
        
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
        BuildingButtonController buttonController = buttonGO.GetComponent<BuildingButtonController>();
        if (buttonController != null)
        {
            buttonController.Initialize(buildingData);
        }
        else
        {
            Debug.LogError("На префабе кнопки нет BuildingButtonController!");
        }
    }
    
    void TogglePanel()
    {
        _isPanelOpen = !_isPanelOpen;
        
        if (panel != null)
            panel.SetActive(_isPanelOpen);
    }
    
    // Для обновления доступности кнопок при изменении ресурсов
    void Update()
    {
        if (_isPanelOpen && _resourceManager != null)
        {
            UpdateButtonsAvailability();
        }
    }

    void UpdateButtonsAvailability()
    {
        foreach (Transform child in buttonsContainer)
        {
            BuildingButtonController buttonController = child.GetComponent<BuildingButtonController>();
            if (buttonController != null)
            {
                // Нужно получить BuildingData для этой кнопки
                // Это можно сделать, если сохранять ссылку в кнопке
            }
        }
    }

    private void InitializeBuildingButtons()
    {
        if (buttonsContainer == null || buildingButtonPrefab == null) return;

        // Очищаем контейнер
        foreach (Transform child in buttonsContainer)
            Destroy(child.gameObject);

        // Создаем кнопки для каждого здания
        foreach (BuildingData buildingData in availableBuildings)
        {
            if (buildingData == null) continue;

            CreateBuildingButton(buildingData);
        }
    }

    private void CreateBuildingButton(BuildingData data)
    {
        GameObject buttonGO = Instantiate(buildingButtonPrefab, buttonsContainer);
        Button button = buttonGO.GetComponent<Button>();

        if (button == null) return;


        // Настраиваем иконку
        Image iconImage = buttonGO.GetComponent<Image>();
        if (iconImage != null && data.Icon != null)
            iconImage.sprite = data.Icon;

        // Настраиваем текст
        TextMeshProUGUI[] texts = buttonGO.GetComponentsInChildren<TextMeshProUGUI>();
        if (texts.Length > 0)
        {
            // Первый текст - название
            texts[0].text = data.DisplayName;

            // Второй текст - стоимость (только если есть стоимость)
            if (texts.Length > 1)
            {
                if (data.ConstructionCost.Resources != null && data.ConstructionCost.Resources.Count > 0)
                {
                    StringBuilder costText = new StringBuilder();
                    foreach (var resource in data.ConstructionCost.Resources)
                    {
                        costText.AppendLine($"{resource.Type}: {resource.Amount}");
                    }
                    texts[1].text = costText.ToString();
                }
                else
                {
                    texts[1].text = "Бесплатно";
                }
            }
        }

        // Сохраняем кнопку
        _buildingButtons[data] = button;

        // Назначаем обработчик клика
        button.onClick.AddListener(() =>
        {
            if (_builderSystem != null)
            {
                _builderSystem.StartPlacement(data);
                // Закрываем панель после выбора
                TogglePanel();
            }
        });

        // Обновляем доступность кнопки
        UpdateButtonAvailability(data, button);
    }

    private void UpdateButtonAvailability(BuildingData data, Button button)
    {
        if (button == null || data == null || _resourceManager == null) return;

        // Проверяем, есть ли стоимость строительства
        bool hasCost = data.ConstructionCost.Resources != null && data.ConstructionCost.Resources.Count > 0;

        if (hasCost)
        {
            bool canAfford = _resourceManager.HasResources(data.ConstructionCost);
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
            CreateBuildingButton(buildingData);
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