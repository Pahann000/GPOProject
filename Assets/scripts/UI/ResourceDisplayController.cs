using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Контроллер для отображения информации об одном типе ресурса
/// Управляет иконкой, текстом и визуальными эффектами
/// </summary>
public class ResourceDisplayController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject warningIcon;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowColor = Color.red;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Header("Warning Settings")]
    [SerializeField] private float lowThreshold = 0.2f;    // 20% от лимита
    [SerializeField] private float warningThreshold = 0.5f; // 50% от лимита
    [SerializeField] private bool enablePulseEffect = true;
    [SerializeField] private float pulseSpeed = 2f;

    private ResourceType resourceType;
    private bool isLow = false;
    private float pulseTimer = 0f;

    /// <summary>
    /// Инициализация контроллера
    /// </summary>
    /// <param name="type">Тип ресурса</param>
    /// <param name="icon">Спрайт иконки</param>
    public void Initialize(ResourceType type, Sprite icon)
    {
        resourceType = type;

        // Устанавливаем иконку
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
        }

        // Настраиваем цвет фона в зависимости от типа ресурса
        if (backgroundImage != null)
        {
            backgroundImage.color = GetBackgroundColorForResourceType(type);
        }

        // Скрываем предупреждение по умолчанию
        if (warningIcon != null)
        {
            warningIcon.SetActive(false);
        }

        // Устанавливаем начальный текст
        UpdateDisplay(0, 100);
    }

    /// <summary>
    /// Обновляет отображение количества ресурса
    /// </summary>
    /// <param name="currentAmount">Текущее количество</param>
    /// <param name="storageLimit">Лимит хранилища</param>
    public void UpdateDisplay(int currentAmount, int storageLimit)
    {
        if (amountText != null)
        {
            amountText.text = $"{currentAmount}/{storageLimit}";
            UpdateTextColor(currentAmount, storageLimit);
        }

        UpdateWarningState(currentAmount, storageLimit);
    }

    /// <summary>
    /// Обновляет цвет текста в зависимости от количества ресурсов
    /// </summary>
    private void UpdateTextColor(int currentAmount, int storageLimit)
    {
        if (amountText == null) return;

        float percentage = storageLimit > 0 ? (float)currentAmount / storageLimit : 1f;

        if (percentage <= lowThreshold)
        {
            amountText.color = lowColor;
            isLow = true;
        }
        else if (percentage <= warningThreshold)
        {
            amountText.color = warningColor;
            isLow = false;
        }
        else
        {
            amountText.color = normalColor;
            isLow = false;
        }
    }

    /// <summary>
    /// Обновляет состояние предупреждения
    /// </summary>
    private void UpdateWarningState(int currentAmount, int storageLimit)
    {
        if (warningIcon == null) return;

        float percentage = storageLimit > 0 ? (float)currentAmount / storageLimit : 1f;
        bool shouldShowWarning = percentage <= lowThreshold;

        warningIcon.SetActive(shouldShowWarning);

        // Если ресурсов мало, можно добавить вибрацию или другие эффекты
        if (shouldShowWarning)
        {
            PlayLowResourceEffect();
        }
    }

    /// <summary>
    /// Эффект для низкого уровня ресурсов
    /// </summary>
    private void PlayLowResourceEffect()
    {
        // Можно добавить звук, анимацию и т.д.
        // Debug.Log($"Внимание! Ресурс {resourceType} на низком уровне!");
    }

    /// <summary>
    /// Возвращает цвет фона в зависимости от типа ресурса
    /// </summary>
    private Color GetBackgroundColorForResourceType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Ice:
                return new Color(0.7f, 0.7f, 0.8f, 0.3f); // Светло-синий
            case ResourceType.Minerals:
                return new Color(0.8f, 0.7f, 0.5f, 0.3f); // Светло-коричневый
            case ResourceType.Rock:
                return new Color(0.7f, 0.8f, 1f, 0.3f);   // Светло-голубой
            case ResourceType.Root:
                return new Color(0.8f, 1f, 0.8f, 0.3f);   // Светло-зеленый
            case ResourceType.Energy:
                return new Color(1f, 1f, 0.7f, 0.3f);     // Светло-желтый
            default:
                return backgroundColor;
        }
    }

    /// <summary>
    /// Устанавливает кастомную иконку
    /// </summary>
    public void SetIcon(Sprite newIcon)
    {
        if (iconImage != null && newIcon != null)
        {
            iconImage.sprite = newIcon;
        }
    }

    /// <summary>
    /// Устанавливает кастомный цвет фона
    /// </summary>
    public void SetBackgroundColor(Color newColor)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = newColor;
        }
    }

    /// <summary>
    /// Включает/выключает отображение
    /// </summary>
    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    /// <summary>
    /// Показывает анимацию при изменении количества ресурсов
    /// </summary>
    public void PlayChangeAnimation(bool isIncrease)
    {
        if (isIncrease)
        {
            // Анимация для увеличения ресурсов
            PlayIncreaseAnimation();
        }
        else
        {
            // Анимация для уменьшения ресурсов
            PlayDecreaseAnimation();
        }
    }

    /// <summary>
    /// Анимация для увеличения ресурсов
    /// </summary>
    private void PlayIncreaseAnimation()
    {
        // Можно добавить LeanTween или другие анимации
        // Например: увеличение масштаба и возврат
        if (gameObject.activeInHierarchy)
        {
            // LeanTween.scale(gameObject, Vector3.one * 1.2f, 0.2f)
            //     .setEase(LeanTweenType.easeOutBack)
            //     .setOnComplete(() => LeanTween.scale(gameObject, Vector3.one, 0.1f));
        }
    }

    /// <summary>
    /// Анимация для уменьшения ресурсов
    /// </summary>
    private void PlayDecreaseAnimation()
    {
        // Можно добавить вибрацию или изменение цвета
        if (gameObject.activeInHierarchy)
        {
            // LeanTween.color(amountText.gameObject, Color.red, 0.1f)
            //     .setOnComplete(() => LeanTween.color(amountText.gameObject, amountText.color, 0.1f));
        }
    }

    /// <summary>
    /// Обновление визуальных эффектов
    /// </summary>
    private void Update()
    {
        UpdatePulseEffect();
    }

    /// <summary>
    /// Эффект пульсации для низкого уровня ресурсов
    /// </summary>
    private void UpdatePulseEffect()
    {
        if (!enablePulseEffect || !isLow || amountText == null) return;

        pulseTimer += Time.deltaTime * pulseSpeed;
        float alpha = (Mathf.Sin(pulseTimer) + 1f) * 0.5f; // От 0 до 1

        Color currentColor = amountText.color;
        currentColor.a = Mathf.Lerp(0.3f, 1f, alpha);
        amountText.color = currentColor;

        // Пульсация для иконки предупреждения
        if (warningIcon != null)
        {
            warningIcon.transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.2f, alpha);
        }
    }

    /// <summary>
    /// Получает тип ресурса, который отображает этот контроллер
    /// </summary>
    public ResourceType GetResourceType()
    {
        return resourceType;
    }

    /// <summary>
    /// Сбрасывает состояние контроллера
    /// </summary>
    public void Reset()
    {
        isLow = false;
        pulseTimer = 0f;

        if (amountText != null)
        {
            amountText.color = normalColor;
        }

        if (warningIcon != null)
        {
            warningIcon.SetActive(false);
            warningIcon.transform.localScale = Vector3.one;
        }
    }
}