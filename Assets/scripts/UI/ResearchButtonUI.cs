using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

/// <summary>
/// Строка древа исследований: название, стоимость, время и состояние
/// (изучено / идёт / доступно / закрыто требованиями).
/// </summary>
public class ResearchButtonUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Button button;

    [Header("Спрайты состояния (9-slice)")]
    [SerializeField] private Sprite stateAvailable;
    [SerializeField] private Sprite stateHover;
    [SerializeField] private Sprite stateInProgress;
    [SerializeField] private Sprite stateCompleted;
    [SerializeField] private Sprite stateLocked;

    [Header("Цвета текста")]
    [SerializeField] private Color availableColor = new Color(0.92f, 0.95f, 1f, 1f);
    [SerializeField] private Color inProgressColor = new Color(0.98f, 0.82f, 0.55f, 1f);
    [SerializeField] private Color completedColor = new Color(0.66f, 0.92f, 0.7f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.66f, 0.68f, 0.72f, 1f);

    private ResearchProject _project;
    private int _index;
    private ResearchPanelUI _panel;

    private readonly StringBuilder _builder = new StringBuilder();

    /// <summary>Проект этой строки.</summary>
    public ResearchProject Project => _project;

    /// <summary>Индекс в атласе.</summary>
    public int Index => _index;

    public void Initialize(ResearchProject project, int index, ResearchPanelUI panel)
    {
        _project = project;
        _index = index;
        _panel = panel;

        if (button == null) button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }

        // Подсветка строки при наведении (общий компонент со списком зданий).
        var hoverHandler = GetComponent<BuildingButtonHover>();
        if (hoverHandler == null) hoverHandler = gameObject.AddComponent<BuildingButtonHover>();
        if (button != null) hoverHandler.Initialize(button, stateHover, backgroundImage);

        if (iconImage != null && project != null && project.icon != null)
        {
            iconImage.sprite = project.icon;
            iconImage.color = Color.white;
        }

        Refresh();
    }

    private void OnClicked()
    {
        if (_panel == null || _project == null) return;

        if (!CanStart()) return;

        _panel.StartResearch(_index);
    }

    private bool CanStart()
    {
        if (ResearchClientState.IsCompleted(_index)) return false;
        if (ResearchClientState.CurrentIndex != ResearchRules.NoResearch) return false;

        return _project.ArePrerequisitesMet(ResearchClientState.Completed, ResearchAtlas.Cached);
    }

    /// <summary>Обновляет подписи, цвет и доступность строки по клиентскому снимку.</summary>
    public void Refresh()
    {
        if (_project == null) return;

        bool completed = ResearchClientState.IsCompleted(_index);
        bool isCurrent = ResearchClientState.CurrentIndex == _index;
        bool prerequisitesMet = _project.ArePrerequisitesMet(ResearchClientState.Completed, ResearchAtlas.Cached);
        bool available = !completed && !isCurrent && prerequisitesMet
            && ResearchClientState.CurrentIndex == ResearchRules.NoResearch;

        string prefix = completed ? "✔ " : isCurrent ? "⏳ " : prerequisitesMet ? "▫ " : "🔒 ";

        Color textColor = completed ? completedColor : isCurrent ? inProgressColor : prerequisitesMet ? availableColor : lockedColor;

        if (nameText != null)
        {
            nameText.text = prefix + _project.DisplayName;
            nameText.color = textColor;
        }

        if (costText != null)
        {
            costText.text = FormatDetails(completed, isCurrent, prerequisitesMet);
            costText.color = completed ? completedColor : isCurrent ? inProgressColor : prerequisitesMet
                ? new Color(0.76f, 0.82f, 0.9f, 1f)
                : lockedColor;
        }

        if (button != null) button.interactable = available;

        ApplyBackground(completed, isCurrent, prerequisitesMet);
    }

    private void ApplyBackground(bool completed, bool isCurrent, bool prerequisitesMet)
    {
        if (backgroundImage == null) return;

        Sprite sprite = completed ? stateCompleted
            : isCurrent ? stateInProgress
            : prerequisitesMet ? stateAvailable
            : stateLocked;

        if (sprite != null)
        {
            backgroundImage.sprite = sprite;
            backgroundImage.type = Image.Type.Sliced;
            backgroundImage.color = Color.white;
        }
    }

    /// <summary>Стоимость, время и причина недоступности одной строкой.</summary>
    private string FormatDetails(bool completed, bool isCurrent, bool prerequisitesMet)
    {
        if (completed) return "Изучено";

        if (isCurrent) return $"Идёт… {ResearchClientState.Progress01 * 100f:F0}%";

        if (!prerequisitesMet) return "Требуются исследования";

        _builder.Clear();

        if (_project.cost != null && _project.cost.Length > 0)
        {
            for (int i = 0; i < _project.cost.Length; i++)
            {
                ResourcePair pair = _project.cost[i];
                if (i > 0) _builder.Append(", ");
                _builder.Append(pair.Type).Append(": ").Append(pair.Amount);
            }
        }
        else
        {
            _builder.Append("Бесплатно");
        }

        _builder.Append("  ·  ").Append(_project.Duration.ToString("F0")).Append(" с");

        return _builder.ToString();
    }
}
