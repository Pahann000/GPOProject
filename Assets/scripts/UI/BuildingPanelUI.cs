using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Окно строительства: список доступных зданий с ценой и замками для
/// неоткрытых исследований.
/// </summary>
public class BuildingPanelUI : UIPanelBase
{
    [Header("Список зданий")]
    [SerializeField] private GameObject buildingButtonPrefab;
    [SerializeField] private Transform buttonsContainer;
    [SerializeField] private List<BuildingData> availableBuildings = new List<BuildingData>();

    [Header("Подвал окна")]
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private string defaultHint = "Клик по зданию — начать постройку. ПКМ или Esc — отмена.";

    private readonly Dictionary<BuildingData, BuildingButtonUI> _buttons = new Dictionary<BuildingData, BuildingButtonUI>();
    private int _researchRevision = int.MinValue;

    public override string PanelTitle => "Строительство";

    /// <summary>Доступные для постройки данные (только чтение).</summary>
    public IReadOnlyList<BuildingData> AvailableBuildings => availableBuildings;

    protected override void Awake()
    {
        base.Awake();

        if (hintText != null) hintText.text = defaultHint;
    }

    /// <summary>Создаёт кнопки один раз (список зданий не меняется на лету).</summary>
    private void EnsureButtons()
    {
        if (_buttons.Count > 0) return;
        if (buttonsContainer == null || buildingButtonPrefab == null)
        {
            Debug.LogError("[BuildingPanelUI] Не назначены buttonsContainer или buildingButtonPrefab!");
            return;
        }

        for (int i = buttonsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = buttonsContainer.GetChild(i);
            if (child.GetComponent<BuildingButtonUI>() != null) Destroy(child.gameObject);
        }

        foreach (BuildingData data in availableBuildings)
        {
            if (data == null) continue;

            GameObject instance = Instantiate(buildingButtonPrefab, buttonsContainer);
            BuildingButtonUI button = instance.GetComponent<BuildingButtonUI>();
            if (button == null) continue;

            button.Initialize(data, this);
            _buttons[data] = button;
        }
    }

    public override void Refresh()
    {
        EnsureButtons();

        // Доступность зависит и от ресурсов, и от открытых исследований.
        bool researchChanged = _researchRevision != ResearchClientState.Revision;
        _researchRevision = ResearchClientState.Revision;

        foreach (var pair in _buttons)
        {
            if (pair.Value != null) pair.Value.UpdateAvailability();
        }

        if (researchChanged && hintText != null) hintText.text = defaultHint;
    }

    /// <summary>
    /// Совместимость со старым вызовом из кнопки здания: после выбора здания
    /// окно закрывается, чтобы не перекрывать поле.
    /// </summary>
    public void SetBuildingsVisible(bool visible)
    {
        if (visible) Open();
        else Close();
    }

    /// <summary>Добавляет здание в список (расширяемость: моды, награды, события).</summary>
    public void AddBuilding(BuildingData buildingData)
    {
        if (buildingData == null || availableBuildings.Contains(buildingData)) return;

        availableBuildings.Add(buildingData);
        _buttons.Clear();
        EnsureButtons();
    }

    /// <summary>Убирает здание из списка.</summary>
    public void RemoveBuilding(BuildingData buildingData)
    {
        if (buildingData == null || !availableBuildings.Remove(buildingData)) return;

        if (_buttons.TryGetValue(buildingData, out BuildingButtonUI button) && button != null)
        {
            Destroy(button.gameObject);
        }

        _buttons.Remove(buildingData);
    }
}
