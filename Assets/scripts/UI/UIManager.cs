using UnityEngine;

/// <summary>
/// Фасад интерфейса: горячие клавиши, доступ к окнам и подсказкам.
/// </summary>
/// <remarks>
/// Видимостью окон управляет <see cref="UIPanelController"/>, а панели сами
/// знают, как отрисовать содержимое (<see cref="UIPanelBase"/>). Поэтому чтобы
/// добавить новое окно, достаточно зарегистрировать его в контроллере — этот
/// класс менять не нужно, кроме (при желании) горячей клавиши.
/// </remarks>
public class UIManager : MonoBehaviour
{
    [Header("Окна и слои")]
    [SerializeField] private UIPanelController panelController;
    [SerializeField] private BuildingPanelUI buildingPanel;
    [SerializeField] private ResearchPanelUI researchPanel;
    [SerializeField] private SelectionPanelUI selectionPanel;
    [SerializeField] private AdvancedTooltip tooltip;

    [Header("Горячие клавиши")]
    [SerializeField] private KeyCode buildingMenuKey = KeyCode.B;
    [SerializeField] private KeyCode researchMenuKey = KeyCode.R;
    [SerializeField] private KeyCode closeMenuKey = KeyCode.Escape;

    /// <summary>Контроллер окон.</summary>
    public UIPanelController PanelController => panelController;

    public BuildingPanelUI BuildingPanel => buildingPanel;
    public ResearchPanelUI ResearchPanel => researchPanel;

    private void Awake()
    {
        // Автопоиск на случай, если ссылки не проставлены в префабе.
        if (panelController == null) panelController = GetComponent<UIPanelController>();
        if (panelController == null) panelController = FindFirstObjectByType<UIPanelController>();

        if (buildingPanel == null) buildingPanel = GetComponentInChildren<BuildingPanelUI>(true);
        if (researchPanel == null) researchPanel = GetComponentInChildren<ResearchPanelUI>(true);
        if (selectionPanel == null) selectionPanel = GetComponentInChildren<SelectionPanelUI>(true);
        if (tooltip == null) tooltip = GetComponentInChildren<AdvancedTooltip>(true);
    }

    private void Start()
    {
        Debug.Log("[UIManager] Готов к работе.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(buildingMenuKey)) ToggleBuildingMenu();
        if (Input.GetKeyDown(researchMenuKey)) ToggleResearchMenu();

        if (Input.GetKeyDown(closeMenuKey) && panelController != null) panelController.CloseAll();
    }

    // ---------------------------------------------------------------- окна

    public void ToggleBuildingMenu()
    {
        if (panelController != null) panelController.Toggle(buildingPanel);
    }

    public void ToggleResearchMenu()
    {
        if (panelController != null) panelController.Toggle(researchPanel);
    }

    /// <summary>Закрывает модальные окна (например, когда игрок начал строить).</summary>
    public void CloseMenus()
    {
        if (panelController != null) panelController.CloseForGameplay();
    }

    /// <summary>Открыто ли какое-либо модальное окно.</summary>
    public bool HasOpenMenu => panelController != null && panelController.OpenedPanel != null;

    // ---------------------------------------------------------------- прочее

    public void ShowNotification(string message, float duration = 3f)
    {
        Debug.Log($"[UI Notification] {message}");
    }

    public void ShowTooltip(string title, string description, string requirements)
    {
        if (tooltip != null) tooltip.ShowTooltip(title, description, requirements);
    }

    public void HideTooltip()
    {
        if (tooltip != null) tooltip.HideTooltip();
    }
}
