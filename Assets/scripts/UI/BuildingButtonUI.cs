using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Кнопка здания в окне строительства: показывает цену, решает, доступно ли
/// здание (открыто исследованием и хватает ли ресурсов).
/// </summary>
public class BuildingButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Спрайты состояния (9-slice)")]
    [SerializeField] private Sprite stateNormal;
    [SerializeField] private Sprite stateHover;
    [SerializeField] private Sprite stateLocked;
    [SerializeField] private Sprite stateUnaffordable;

    [Header("Цвета")]
    [SerializeField] private Color affordableIconColor = Color.white;
    [SerializeField] private Color unaffordableIconColor = new Color(0.62f, 0.66f, 0.72f, 1f);
    [SerializeField] private Color lockedTextColor = new Color(0.72f, 0.72f, 0.76f, 1f);

    private BuildingData _buildingData;
    private BuildingPanelUI _parentPanel;
    private string _costString = string.Empty;

    /// <summary>Открыт ли тип здания исследованием.</summary>
    public bool IsUnlocked => _buildingData == null || ResearchClientState.IsBuildingUnlocked(_buildingData.BuildingTypeName);

    public void Initialize(BuildingData data, BuildingPanelUI parentPanel)
    {
        _buildingData = data;
        _parentPanel = parentPanel;

        if (button == null) button = GetComponent<Button>();

        if (iconImage != null && data.Icon != null) iconImage.sprite = data.Icon;

        _costString = FormatCost(data);
        if (costText != null) costText.text = _costString;

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }

        var hoverHandler = GetComponent<BuildingButtonHover>();
        if (hoverHandler == null) hoverHandler = gameObject.AddComponent<BuildingButtonHover>();
        if (button != null) hoverHandler.Initialize(button, stateHover, backgroundImage);

        UpdateAvailability();
    }

    private static string FormatCost(BuildingData data)
    {
        if (data.ConstructionCost == null || data.ConstructionCost.Length == 0) return "Бесплатно";

        var builder = new System.Text.StringBuilder();
        for (int i = 0; i < data.ConstructionCost.Length; i++)
        {
            if (i > 0) builder.Append("  ");
            builder.Append(data.ConstructionCost[i].Type).Append(": ").Append(data.ConstructionCost[i].Amount);
        }

        return builder.ToString();
    }

    private void OnButtonClicked()
    {
        if (_buildingData == null || !IsUnlocked) return;

        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (kernel.TryGetSystem(out BuilderSystem builder))
        {
            builder.StartPlacement(_buildingData);
        }

        // Окно закрывается, чтобы не перекрывать поле во время постройки.
        if (kernel.TryGetSystem(out UISystem ui) && ui.Manager != null)
        {
            ui.Manager.CloseMenus();
        }
        else if (_parentPanel != null)
        {
            _parentPanel.Close();
        }
    }

    /// <summary>Обновляет вид кнопки: замок исследования и доступность по ресурсам.</summary>
    public void UpdateAvailability()
    {
        if (_buildingData == null) return;

        bool unlocked = IsUnlocked;

        Player player = null;
        var kernel = GameKernel.Instance;
        if (kernel != null && kernel.TryGetSystem(out UISystem uiSystem)) player = uiSystem.LocalPlayer;

        bool canAfford = player != null && player.Resources.HasResources(_buildingData.ConstructionCost);

        if (nameText != null)
        {
            nameText.text = unlocked ? _buildingData.DisplayName : "🔒 " + _buildingData.DisplayName;
            nameText.color = unlocked ? Color.white : lockedTextColor;
        }

        if (costText != null)
        {
            costText.text = unlocked ? _costString : "Нужно исследование";
            costText.color = unlocked
                ? (canAfford ? new Color(0.72f, 0.86f, 0.72f, 1f) : new Color(0.86f, 0.56f, 0.5f, 1f))
                : lockedTextColor;
        }

        if (iconImage != null) iconImage.color = unlocked ? (canAfford ? affordableIconColor : unaffordableIconColor) : unaffordableIconColor;

        if (button != null) button.interactable = unlocked && canAfford;

        ApplyBackgroundSprite(!unlocked ? stateLocked : (canAfford ? stateNormal : stateUnaffordable));
    }

    private void ApplyBackgroundSprite(Sprite sprite)
    {
        if (backgroundImage == null) return;

        if (sprite != null)
        {
            backgroundImage.sprite = sprite;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = Color.white;
        }
        else
        {
            backgroundImage.color = new Color(1f, 1f, 1f, 0.9f);
        }
    }
}
