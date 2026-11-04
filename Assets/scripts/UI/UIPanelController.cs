using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Управляет модальными окнами интерфейса: открывает одно окно поверх игры,
/// показывает затемнение, закрывает по Esc и по клику на фон.
/// </summary>
/// <remarks>
/// Окна регистрируются в списке <see cref="panels"/>. Открыто всегда не больше
/// одного — так игрок не теряется в наложенных друг на друга панелях, а игровое
/// поле остаётся видимым, но заблокированным затемнением.
/// </remarks>
public class UIPanelController : MonoBehaviour
{
    [Header("Слои")]
    [Tooltip("Полноэкранное затемнение под окном. Клик по нему закрывает окно.")]
    [SerializeField] private GameObject modalRoot;

    [Tooltip("Кнопка на затемнении (необязательно): закрывает окно по клику.")]
    [SerializeField] private Button dimmerButton;

    [Header("Окна")]
    [SerializeField] private List<UIPanelBase> panels = new List<UIPanelBase>();

    [Header("Управление")]
    [SerializeField] private KeyCode closeKey = KeyCode.Escape;

    [Tooltip("Закрывать окно, когда игрок начинает строить или выделяет объект.")]
    [SerializeField] private bool closeOnGameplayAction = true;

    /// <summary>Текущее открытое окно (null — все закрыты).</summary>
    public UIPanelBase OpenedPanel { get; private set; }

    /// <summary>Индекс открытого окна в списке (для сохранения состояния).</summary>
    public IReadOnlyList<UIPanelBase> Panels => panels;

    private void Awake()
    {
        if (dimmerButton != null) dimmerButton.onClick.AddListener(CloseAll);

        RegisterAll();
        CloseAll();
    }

    private void Update()
    {
        if (OpenedPanel != null && Input.GetKeyDown(closeKey))
        {
            CloseAll();
        }
    }

    /// <summary>
    /// Переписывает список окон (вызывается из <see cref="UIManager"/> после сборки UI).
    /// </summary>
    public void SetPanels(IEnumerable<UIPanelBase> newPanels)
    {
        foreach (UIPanelBase panel in panels)
        {
            if (panel != null) panel.OpenStateChanged -= OnPanelStateChanged;
        }

        panels.Clear();

        if (newPanels != null)
        {
            foreach (UIPanelBase panel in newPanels)
            {
                if (panel != null) panels.Add(panel);
            }
        }

        RegisterAll();
    }

    private void RegisterAll()
    {
        foreach (UIPanelBase panel in panels)
        {
            if (panel == null) continue;

            panel.Controller = this;
            panel.OpenStateChanged -= OnPanelStateChanged;
            panel.OpenStateChanged += OnPanelStateChanged;
            panel.gameObject.SetActive(false);
        }

        UpdateDimmer();
    }

    /// <summary>Открывает окно, закрывая остальные.</summary>
    public void Open(UIPanelBase panel)
    {
        if (panel == null) return;

        foreach (UIPanelBase other in panels)
        {
            if (other != null && other != panel && other.IsOpen) other.Close();
        }

        panel.Open();
        OpenedPanel = panel;
        UpdateDimmer();
    }

    /// <summary>Открывает окно по индексу в списке.</summary>
    public void OpenByIndex(int index)
    {
        if (index < 0 || index >= panels.Count) return;

        Open(panels[index]);
    }

    /// <summary>Переключает окно: открытое закрывает, закрытое открывает.</summary>
    public void Toggle(UIPanelBase panel)
    {
        if (panel == null) return;

        if (panel.IsOpen)
        {
            panel.Close();
        }
        else
        {
            Open(panel);
        }
    }

    /// <summary>Закрывает все окна и убирает затемнение.</summary>
    public void CloseAll()
    {
        OpenedPanel = null;

        foreach (UIPanelBase panel in panels)
        {
            if (panel != null && panel.IsOpen) panel.Close();
        }

        UpdateDimmer();
    }

    /// <summary>
    /// Закрывает окно при действии в мире (начали строить, выбрали здание),
    /// чтобы панель не перекрывала игровое поле.
    /// </summary>
    public void CloseForGameplay()
    {
        if (!closeOnGameplayAction) return;
        if (OpenedPanel == null) return;

        CloseAll();
    }

    private void OnPanelStateChanged(UIPanelBase panel, bool open)
    {
        OpenedPanel = null;

        foreach (UIPanelBase other in panels)
        {
            if (other != null && other.IsOpen)
            {
                OpenedPanel = other;
                break;
            }
        }

        UpdateDimmer();
    }

    private void UpdateDimmer()
    {
        if (modalRoot != null) modalRoot.SetActive(OpenedPanel != null);
    }
}
