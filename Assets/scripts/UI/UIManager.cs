using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Главный контроллер интерфейса. 
/// Позиционирует панели по схеме игрока и накладывает процедурные спрайты.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] public ResourcePanelUI resourcePanel;
    [SerializeField] public BuildingPanelUI buildingPanel;
    [SerializeField] public SelectionPanelUI selectionPanel;
    [SerializeField] public MinimapUI minimapPanel;
    [SerializeField] public AdvancedTooltip tooltip;
    [SerializeField] public Button toggleBuildButton;

    [Header("Input Settings")]
    [SerializeField] private KeyCode buildingMenuKey = KeyCode.B;

    private void Awake()
    {
        // 1. Генерируем текстуры в памяти
        UISpriteGenerator.GenerateAll();

        // 2. Находим панели
        AutoWirePanels();

        // 3. Накладываем процедурные спрайты на рамки и кнопки
        ApplyProceduralSprites();
    }

    private void Start()
    {
        if (toggleBuildButton != null)
        {
            toggleBuildButton.onClick.AddListener(ToggleBuildingMenu);
        }

        Debug.Log("[UIManager] Интерфейс сконфигурирован по схеме.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(buildingMenuKey))
        {
            ToggleBuildingMenu();
        }
    }

    private void AutoWirePanels()
    {
        if (resourcePanel == null) resourcePanel = GetComponentInChildren<ResourcePanelUI>(true);
        if (buildingPanel == null) buildingPanel = GetComponentInChildren<BuildingPanelUI>(true);
        if (selectionPanel == null) selectionPanel = GetComponentInChildren<SelectionPanelUI>(true);
        if (minimapPanel == null) minimapPanel = GetComponentInChildren<MinimapUI>(true);
        if (tooltip == null) tooltip = GetComponentInChildren<AdvancedTooltip>(true);

        if (toggleBuildButton == null)
        {
            var btnObj = transform.Find("ToggleBuildButton");
            if (btnObj != null) toggleBuildButton = btnObj.GetComponent<Button>();
        }
    }

    private void ApplyProceduralSprites()
    {
        // Применяем рамку для панели выделения
        if (selectionPanel != null)
        {
            var img = selectionPanel.GetComponent<Image>();
            if (img != null) { img.sprite = UISpriteGenerator.PanelBackground; img.type = Image.Type.Sliced; }
        }

        // Применяем рамку для панели строительства
        if (buildingPanel != null)
        {
            var img = buildingPanel.GetComponent<Image>();
            if (img != null) { img.sprite = UISpriteGenerator.PanelBackground; img.type = Image.Type.Sliced; }
        }

        // Применяем рамку для ресурсов
        if (resourcePanel != null)
        {
            var img = resourcePanel.GetComponent<Image>();
            if (img != null) { img.sprite = UISpriteGenerator.PanelBackground; img.type = Image.Type.Sliced; }
        }

        // Настраиваем иконку молотка на кнопке строительства
        if (toggleBuildButton != null)
        {
            var btnImg = toggleBuildButton.GetComponent<Image>();
            if (btnImg != null) { btnImg.sprite = UISpriteGenerator.ButtonNormal; btnImg.type = Image.Type.Sliced; }

            // Если внутри кнопки есть дочерний Image для иконки
            var iconImg = toggleBuildButton.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImg != null)
            {
                iconImg.sprite = UISpriteGenerator.HammerIcon;
                iconImg.color = Color.white;
            }
        }
    }

    private void ToggleBuildingMenu()
    {
        if (buildingPanel != null)
        {
            bool currentState = !buildingPanel.gameObject.activeSelf; 
            buildingPanel.SetBuildingsVisible(currentState); 
        }
    }

    // Методы для Tooltip
    public void ShowTooltip(string title, string description, string requirements)
    {
        if (tooltip != null)
            tooltip.ShowTooltip(title, description, requirements);
    }

    public void HideTooltip()
    {
        if (tooltip != null)
            tooltip.HideTooltip();
    }
}