using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] public ResourcePanelUI resourcePanel;
    [SerializeField] public BuildingPanelUI buildingPanel;
    [SerializeField] public SelectionPanelUI selectionPanel;
    [SerializeField] public AdvancedTooltip tooltip;

    [Header("Input Settings")]
    [SerializeField] private KeyCode buildingMenuKey = KeyCode.B;

    private void Start()
    {
        // Автоматический поиск компонентов если не назначены
        if (resourcePanel == null) resourcePanel = GetComponentInChildren<ResourcePanelUI>(true);

        if (buildingPanel == null) buildingPanel = GetComponentInChildren<BuildingPanelUI>(true);

        if (selectionPanel == null) selectionPanel = GetComponentInChildren<SelectionPanelUI>(true);

        if (tooltip != null) tooltip = GetComponentInChildren<AdvancedTooltip>(true);

        Debug.Log("[UIManager] Готов к работе.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(buildingMenuKey))
        {
            ToggleBuildingMenu();
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

    public void ShowNotification(string message, float duration = 3f)
    {
        Debug.Log($"[UI Notification] {message}");
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