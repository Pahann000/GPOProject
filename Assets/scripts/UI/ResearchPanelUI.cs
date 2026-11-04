using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Окно древа исследований: статус текущего исследования с полосой прогресса
/// и список всех исследований.
/// </summary>
public class ResearchPanelUI : UIPanelBase
{
    [Header("Список исследований")]
    [SerializeField] private GameObject researchEntryPrefab;
    [SerializeField] private Transform entriesContainer;

    [Header("Статус текущего исследования")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image progressFill;

    private readonly List<ResearchButtonUI> _entries = new List<ResearchButtonUI>();
    private bool _entriesBuilt;

    public override string PanelTitle => "Исследования";

    protected override void Awake()
    {
        base.Awake();
    }

    public override void Refresh()
    {
        EnsureEntries();
        RefreshStatus();
        RefreshEntries();
    }

    /// <summary>Создаёт строки по атласу. Атлас может быть не готов на первом кадре.</summary>
    private void EnsureEntries()
    {
        if (_entriesBuilt) return;

        ResearchAtlas atlas = ResearchAtlas.Cached;
        if (atlas == null || atlas.Count == 0) return;

        if (entriesContainer == null || researchEntryPrefab == null)
        {
            Debug.LogError("[ResearchPanelUI] Не назначены entriesContainer или researchEntryPrefab!");
            _entriesBuilt = true;
            return;
        }

        for (int i = entriesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(entriesContainer.GetChild(i).gameObject);
        }

        _entries.Clear();

        for (int i = 0; i < atlas.Count; i++)
        {
            ResearchProject project = atlas.Get(i);
            if (project == null) continue;

            GameObject entry = Instantiate(researchEntryPrefab, entriesContainer);
            ResearchButtonUI entryUI = entry.GetComponent<ResearchButtonUI>();

            if (entryUI == null)
            {
                Debug.LogWarning("[ResearchPanelUI] На префабе строки нет ResearchButtonUI.");
                Destroy(entry);
                continue;
            }

            entryUI.Initialize(project, i, this);
            _entries.Add(entryUI);
        }

        _entriesBuilt = true;
        Debug.Log($"[ResearchPanelUI] Построено строк исследований: {_entries.Count}.");
    }

    private void RefreshStatus()
    {
        int current = ResearchClientState.CurrentIndex;

        if (statusText != null)
        {
            if (current == ResearchRules.NoResearch)
            {
                statusText.text = "Выберите исследование";
            }
            else
            {
                ResearchAtlas atlas = ResearchAtlas.Cached;
                ResearchProject project = atlas != null ? atlas.Get(current) : null;
                string name = project != null ? project.DisplayName : $"#{current}";

                statusText.text = $"{name} — {ResearchClientState.Progress01 * 100f:F0}%";
            }
        }

        if (progressFill != null)
        {
            float fill = current == ResearchRules.NoResearch ? 0f : ResearchClientState.Progress01;

            RectTransform rect = progressFill.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    private void RefreshEntries()
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i] != null) _entries[i].Refresh();
        }
    }

    /// <summary>
    /// Запускает исследование по индексу в атласе: команду отправляет Player,
    /// сервер проверяет требования и списывает ресурсы.
    /// </summary>
    public void StartResearch(int index)
    {
        Player player = LocalPlayer();

        if (player == null)
        {
            Debug.LogWarning("[ResearchPanelUI] Локальный игрок ещё не известен.");
            return;
        }

        player.CmdStartResearch(index);
    }

    private static Player LocalPlayer()
    {
        var kernel = GameKernel.Instance;
        if (kernel == null || !kernel.TryGetSystem(out UISystem ui)) return null;

        return ui.LocalPlayer;
    }
}
