using UnityEngine;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Панель ресурсов в HUD: создаёт строку на каждый <see cref="ResourceType"/>
/// и держит её в актуальном состоянии.
/// </summary>
/// <remarks>
/// Раньше панель читала значения только один раз в <c>Start</c>, когда локальный
/// игрок обычно ещё не известен (UI создаётся раньше префаба игрока) — и оставалась
/// с нулями. Теперь она:
/// <list type="bullet">
///   <item>ждёт появления локального игрока и подхватывает его;</item>
///   <item>перечитывает его хранилище, когда значения поменялись — это работает
///   независимо от порядка сетевых событий;</item>
///   <item>по-прежнему реагирует на <see cref="ResourceChangedEvent"/>, чтобы
///   показывать изменения в тот же кадр.</item>
/// </list>
/// Именно поэтому выделенный клиент (отдельный процесс) показывает реальные
/// значения, а не «0/1000» из стартового состояния.
/// </remarks>
public class ResourcePanelUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject resourceDisplayPrefab;
    [SerializeField] private Transform resourcesContainer;

    [Header("Resource Icons")]
    [SerializeField] private Sprite metalIcon;
    [SerializeField] private Sprite mineralsIcon;
    [SerializeField] private Sprite iceIcon;
    [SerializeField] private Sprite foodIcon;
    [SerializeField] private Sprite energyIcon;

    private readonly Dictionary<ResourceType, ResourceDisplayController> _resourceControllers =
        new Dictionary<ResourceType, ResourceDisplayController>();

    /// <summary>Что сейчас показано: нужно, чтобы не обновлять UI каждый кадр.</summary>
    private readonly Dictionary<ResourceType, int> _shownAmounts = new Dictionary<ResourceType, int>();
    private readonly Dictionary<ResourceType, int> _shownLimits = new Dictionary<ResourceType, int>();

    private readonly StringBuilder _logBuilder = new StringBuilder();

    /// <summary>Ограничение частоты лога, чтобы производство не забивало консоль.</summary>
    private const float LogIntervalSeconds = 1f;
    private float _lastLogTime = -100f;

    /// <summary>Была ли уже записана строка с ненулевыми ресурсами.</summary>
    private bool _loggedPositive;

    /// <summary>Игрок, чьи ресурсы сейчас показаны (null — ещё не известен).</summary>
    private Player _shownPlayer;

    private void Start()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Subscribe<ResourceChangedEvent>(OnResourceChangedBus);
        }

        InitializeResourceDisplays();
        RefreshFromPlayer(forceLog: false);
    }

    private void OnDestroy()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChangedBus);
        }
    }

    private void Update()
    {
        // UI создаётся раньше префаба игрока, поэтому в Start показывать нечего.
        // Как только локальный игрок появился (или сменился) — подхватываем его.
        if (_shownPlayer == null || _shownPlayer != CurrentPlayer())
        {
            RefreshFromPlayer(forceLog: true);
            return;
        }

        // Значения могли измениться без события (например, снимок пришёл раньше
        // подписки UI): перечитываем хранилище и обновляем строки при изменении.
        RefreshFromPlayer(forceLog: false);
    }

    private Player CurrentPlayer()
    {
        var kernel = GameKernel.Instance;
        if (kernel == null || !kernel.TryGetSystem(out UISystem uiSystem)) return null;

        return uiSystem.LocalPlayer;
    }

    private void InitializeResourceDisplays()
    {
        if (resourceDisplayPrefab == null || resourcesContainer == null)
        {
            Debug.LogError("[ResourcePanelUI] Не назначены resourceDisplayPrefab или resourcesContainer!");
            return;
        }

        // Очищаем контейнер от старых строк (например, при повторной инициализации).
        for (int i = resourcesContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = resourcesContainer.GetChild(i);
            if (child.GetComponent<ResourceDisplayController>() != null)
            {
                Destroy(child.gameObject);
            }
        }

        _resourceControllers.Clear();
        _shownAmounts.Clear();
        _shownLimits.Clear();

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            GameObject resourceGO = Instantiate(resourceDisplayPrefab, resourcesContainer);
            ResourceDisplayController controller = resourceGO.GetComponent<ResourceDisplayController>();

            if (controller == null) continue;

            controller.Initialize(type, GetIconForResourceType(type));
            _resourceControllers[type] = controller;
        }
    }

    private void OnResourceChangedBus(ResourceChangedEvent evt)
    {
        if (!_resourceControllers.TryGetValue(evt.Type, out ResourceDisplayController controller)) return;

        controller.UpdateDisplay(evt.NewAmount, evt.Limit);

        _shownAmounts[evt.Type] = evt.NewAmount;
        _shownLimits[evt.Type] = evt.Limit;
    }

    /// <summary>
    /// Перечитывает состояние из локального хранилища игрока и обновляет строки.
    /// </summary>
    private void RefreshFromPlayer(bool forceLog)
    {
        Player player = CurrentPlayer();
        if (player == null) return;

        bool playerChanged = player != _shownPlayer;
        _shownPlayer = player;

        bool anyChanged = false;
        bool anyPositive = false;

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            int amount = player.Resources.GetResource(type);
            int limit = player.Resources.GetStorageLimit(type);

            if (amount > 0) anyPositive = true;

            bool changed = playerChanged
                || !_shownAmounts.TryGetValue(type, out int shownAmount) || shownAmount != amount
                || !_shownLimits.TryGetValue(type, out int shownLimit) || shownLimit != limit;

            if (!changed) continue;

            anyChanged = true;
            _shownAmounts[type] = amount;
            _shownLimits[type] = limit;

            if (_resourceControllers.TryGetValue(type, out ResourceDisplayController controller))
            {
                controller.UpdateDisplay(amount, limit);
            }
        }

        // Первое появление ненулевых значений логируем всегда: именно эта строка
        // показывает, что клиент получил реальные ресурсы, а не стартовые нули.
        bool firstPositive = anyPositive && !_loggedPositive;
        if (anyPositive) _loggedPositive = true;

        if (anyChanged || forceLog || firstPositive) LogValues(player, forceLog || firstPositive);
    }

    private void LogValues(Player player, bool force)
    {
        if (!force && Time.unscaledTime - _lastLogTime < LogIntervalSeconds) return;

        _lastLogTime = Time.unscaledTime;

        _logBuilder.Clear();
        _logBuilder.Append("[ResourcePanelUI] Ресурсы игрока: ");

        bool first = true;
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            if (!first) _logBuilder.Append(", ");
            first = false;

            _logBuilder.Append(type)
                .Append('=')
                .Append(player.Resources.GetResource(type))
                .Append('/')
                .Append(player.Resources.GetStorageLimit(type));
        }

        Debug.Log(_logBuilder.ToString());
    }

    private Sprite GetIconForResourceType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Rock: return metalIcon;
            case ResourceType.Minerals: return mineralsIcon;
            case ResourceType.Ice: return iceIcon;
            case ResourceType.Root: return foodIcon;
            case ResourceType.Energy: return energyIcon;
            default: return null;
        }
    }
}
