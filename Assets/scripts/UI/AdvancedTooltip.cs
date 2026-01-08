using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdvancedTooltip : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI requirementsText;

    [Header("Settings")]
    [SerializeField] private float showDelay = 0.5f;
    [SerializeField] private Vector2 offset = new Vector2(20, -20);

    private Canvas parentCanvas;
    private float showTimer;
    private bool wantsToShow;

    private void Start()
    {
        parentCanvas = GetComponentInParent<Canvas>();

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (wantsToShow)
        {
            showTimer += Time.deltaTime;
            if (showTimer >= showDelay && tooltipPanel != null && !tooltipPanel.activeSelf)
            {
                tooltipPanel.SetActive(true);
                UpdatePosition();
            }
        }

        if (tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdatePosition();
        }
    }

    public void ShowTooltip(string title, string description, string requirements)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
        if (requirementsText != null) requirementsText.text = requirements;

        wantsToShow = true;
        showTimer = 0f;
    }

    public void HideTooltip()
    {
        wantsToShow = false;
        showTimer = 0f;

        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private void UpdatePosition()
    {
        if (tooltipPanel == null || parentCanvas == null) return;

        Vector2 mousePos = Input.mousePosition;
        Vector2 tooltipPos;

        // Конвертируем позицию мыши в локальные координаты канваса
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePos,
            parentCanvas.worldCamera,
            out tooltipPos
        );

        // Применяем смещение
        tooltipPos += offset;

        // Устанавливаем позицию
        tooltipPanel.transform.localPosition = tooltipPos;
    }
}