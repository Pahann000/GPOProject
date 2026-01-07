using UnityEngine;
using TMPro;

public class Tooltip : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI tooltipText; 

    [Header("Settings")]
    [SerializeField] private bool showOnHover = true;
    [SerializeField] private float showDelay = 0.5f;

    private GameObject tooltipObject;
    private bool isShowing = false;

    private void Start()
    {
        // Если текстовый объект не назначен, попробуем найти его
        if (tooltipText == null)
        {
            tooltipText = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Находим родительский объект тултипа
        if (tooltipText != null)
        {
            tooltipObject = tooltipText.gameObject;
            tooltipObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"Tooltip: TextMeshProUGUI не найден на {gameObject.name}");
        }
    }

    /// <summary>
    /// Устанавливает текст тултипа
    /// </summary>
    public void SetText(string text)
    {
        if (tooltipText != null)
        {
            tooltipText.text = text;
        }
        else
        {
            Debug.LogError($"Tooltip.SetText: tooltipText is null on {gameObject.name}");
        }
    }

    /// <summary>
    /// Показывает тултип
    /// </summary>
    public void Show()
    {
        if (tooltipObject != null && !isShowing)
        {
            tooltipObject.SetActive(true);
            isShowing = true;
        }
    }

    /// <summary>
    /// Скрывает тултип
    /// </summary>
    public void Hide()
    {
        if (tooltipObject != null && isShowing)
        {
            tooltipObject.SetActive(false);
            isShowing = false;
        }
    }

    // Опционально: можно добавить обработчики наведения мыши
    public void OnPointerEnter()
    {
        if (showOnHover && !string.IsNullOrEmpty(tooltipText?.text))
        {
            Invoke(nameof(Show), showDelay);
        }
    }

    public void OnPointerExit()
    {
        if (showOnHover)
        {
            CancelInvoke(nameof(Show));
            Hide();
        }
    }
}