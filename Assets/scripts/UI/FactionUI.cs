using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Показывает в HUD фракцию локального игрока: название и цвет фракции.
/// </summary>
/// <remarks>
/// Фракция назначается сервером асинхронно (SyncVar), поэтому компонент
/// не кэширует её при старте, а следит за изменением каждый кадр и обновляет
/// только при реальной смене значения.
/// </remarks>
public class FactionUI : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private TextMeshProUGUI factionLabel;
    [SerializeField] private Image factionAccent;

    [Header("Настройки")]
    [SerializeField] private string labelFormat = "Фракция: {0}";
    [SerializeField] private string unassignedText = "Фракция: не назначена";

    private FactionType _shownFaction = FactionType.None;
    private bool _initialized;

    private void Update()
    {
        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (!kernel.TryGetSystem(out UISystem uiSystem)) return;

        Player player = uiSystem.LocalPlayer;
        if (player == null) return;

        if (_initialized && player.Faction == _shownFaction) return;

        Apply(player.Faction);
    }

    /// <summary>
    /// Применяет фракцию к HUD. Вызывается при первом появлении локального
    /// игрока и при каждой смене фракции.
    /// </summary>
    public void Apply(FactionType faction)
    {
        _shownFaction = faction;
        _initialized = true;

        FactionDefinition definition = FactionAtlas.GetDefinition(faction);

        if (factionLabel != null)
        {
            factionLabel.text = definition != null && !string.IsNullOrEmpty(definition.DisplayName)
                ? string.Format(labelFormat, definition.DisplayName)
                : unassignedText;

            // Цвет фракции сильно осветляем и добавляем тёмную обводку: подпись
            // живёт на тёмной верхней панели и должна читаться в любом окружении.
            if (definition != null)
            {
                factionLabel.color = Color.Lerp(definition.PrimaryColor, Color.white, 0.78f);
                factionLabel.outlineColor = new Color32(10, 12, 18, 255);
                factionLabel.outlineWidth = 0.22f;
            }
        }

        if (factionAccent != null)
        {
            factionAccent.color = definition != null ? definition.PrimaryColor : new Color(1f, 1f, 1f, 0.2f);
        }
    }
}
