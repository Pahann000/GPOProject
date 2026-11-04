using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Базовая панель интерфейса (окно). Наследники описывают только содержимое:
/// заголовок, обновление и (при необходимости) поведение при открытии.
/// </summary>
/// <remarks>
/// Раньше каждая панель сама управляла своей видимостью и своей кнопкой-переключателем,
/// из-за чего добавлять новые окна было неудобно, а открытые панели могли висеть
/// друг на друге. Теперь видимостью управляет <see cref="UIPanelController"/>,
/// а панель лишь сообщает ему о закрытии.
/// </remarks>
public abstract class UIPanelBase : MonoBehaviour
{
    [Header("Общие элементы окна")]
    [Tooltip("Заголовок окна в шапке.")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Tooltip("Кнопка закрытия в шапке.")]
    [SerializeField] private Button closeButton;

    /// <summary>Заголовок окна (используется, если в префабе не задан текст).</summary>
    public abstract string PanelTitle { get; }

    /// <summary>Окно открыто.</summary>
    public bool IsOpen => gameObject.activeSelf;

    /// <summary>Контроллер окон, который управляет этой панелью.</summary>
    public UIPanelController Controller { get; internal set; }

    /// <summary>Сообщает контроллеру об изменении состояния (открыто/закрыто).</summary>
    public event Action<UIPanelBase, bool> OpenStateChanged;

    protected virtual void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (titleText != null && !string.IsNullOrEmpty(PanelTitle)) titleText.text = PanelTitle;
    }

    /// <summary>Открывает окно и обновляет содержимое.</summary>
    public virtual void Open()
    {
        gameObject.SetActive(true);
        ApplyTitle();
        Refresh();
        OpenStateChanged?.Invoke(this, true);
    }

    /// <summary>Закрывает окно.</summary>
    public virtual void Close()
    {
        if (!gameObject.activeSelf) return;

        gameObject.SetActive(false);
        OpenStateChanged?.Invoke(this, false);
    }

    /// <summary>Обновляет содержимое окна. Вызывается каждый кадр, пока окно открыто.</summary>
    public abstract void Refresh();

    /// <summary>
    /// Пока окно открыто, каждый кадр обновляем содержимое: прогресс исследований,
    /// доступность зданий и ресурсы меняются независимо от действий игрока.
    /// </summary>
    protected virtual void Update()
    {
        if (!gameObject.activeInHierarchy) return;

        Refresh();
    }

    /// <summary>Устанавливает заголовок в шапке (например, с именем здания).</summary>
    protected void SetTitle(string title)
    {
        if (titleText != null) titleText.text = title;
    }

    private void ApplyTitle()
    {
        if (titleText != null) titleText.text = PanelTitle;
    }
}
