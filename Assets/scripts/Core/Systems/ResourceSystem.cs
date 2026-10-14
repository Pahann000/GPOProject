using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет экономикой игры: хранением, начислением и списанием ресурсов.
/// Рассылает события (ResourceChangedEvent) при любом изменении баланса.
/// </summary>
public class ResourceSystem : IGameSystem, IResourceProvider
{
    public string SystemName => nameof(ResourceSystem);
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;

    private Dictionary<Player, ResourceBundle> _playersResources = new Dictionary<Player, ResourceBundle>();

    /// <summary>
    /// Все типы ресурсов: нужны бонусам, которые касаются складов целиком
    /// (главное здание увеличивает лимит сразу по всем ресурсам).
    /// </summary>
    private static readonly ResourceType[] AllResourceTypes =
        (ResourceType[])System.Enum.GetValues(typeof(ResourceType));

    // TODO: В будущем можно вынести стартовые значения и лимиты в отдельный ScriptableObject (Config),
    // чтобы можно было настраивать без перекомпиляции кода.
    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        // TODO: Здесь можно реализовать пассивный расход ресурсов со временем
        // (например, еда уходит на пропитание юнитов каждую секунду).
    }

    public void FixedTick(float deltaTime) { }

    public void Shutdown()
    {
        var clientNotifier = GetNotifier();
        if (clientNotifier != null)
        {
            // Отписываемся от событий для всех игроков
            foreach (var player in _playersResources.Keys)
            {
                clientNotifier.Unsubscribe(player);
            }
        }

        _playersResources.Clear();
    }

    /// <summary>
    /// Задает начальные значения и лимиты для всех существующих типов ресурсов.
    /// Повторный вызов (реконнект) безопасен.
    /// </summary>
    public void RegisterPlayer(Player player)
    {
        if (player == null) return;

        if (!_playersResources.ContainsKey(player))
        {
            _playersResources.Add(player, new ResourceBundle());
        }

        GetNotifier()?.Subscribe(player);
    }

    /// <summary>
    /// Убирает игрока из экономики. Player — это NetworkBehaviour, и после его
    /// уничтожения запись в словаре оставалась мусором навсегда.
    /// </summary>
    public void UnregisterPlayer(Player player)
    {
        if (player == null) return;

        _playersResources.Remove(player);
        GetNotifier()?.Unsubscribe(player);
    }

    private ResourceChangedClientNotifier GetNotifier()
    {
        return _kernel != null && _kernel.TryGetSystem(out ResourceChangedClientNotifier notifier)
            ? notifier
            : null;
    }

    /// <summary>
    /// Безопасно достает хранилище игрока: прямой индексатор бросал
    /// KeyNotFoundException для незарегистрированного или уже уничтоженного игрока.
    /// </summary>
    private bool TryGetBundle(Player player, out ResourceBundle bundle)
    {
        bundle = null;

        if (player == null) return false;

        return _playersResources.TryGetValue(player, out bundle) && bundle != null;
    }

    /// <summary>
    /// Проверяет, достаточно ли ресурсов в хранилище для оплаты указанной цены.
    /// </summary>
    public bool HasResources(Player player, ResourcePair[] cost)
    {
        return TryGetBundle(player, out var bundle) && bundle.HasResources(cost);
    }

    public bool HasResource(Player player, ResourcePair cost)
    {
        return TryGetBundle(player, out var bundle) && bundle.HasResource(cost);
    }

    /// <summary>
    /// Пытается списать ресурсы. Если их недостаточно, операция отменяется.
    /// </summary>
    /// <returns>True, если ресурсы успешно списаны.</returns>
    public bool TrySpendResources(Player player, ResourcePair[] outcome)
    {
        // Пустой список расходов — операция успешна (например, бесплатное здание).
        if (outcome == null || outcome.Length == 0) return true;

        if (!HasResources(player, outcome)) 
        {
            Debug.LogWarning($"[{SystemName}] Попытка списать ресурсы провалена: недостаточно средств.");
            return false; 
        }

        foreach (var resPair in outcome)
        {
            SpendResourceInternal(player, resPair);
        }

        return true;
    }

    /// <summary>
    /// Начисляет ресурсы (например, доход от здания).
    /// </summary>
    public void AddResources(Player player, ResourcePair[] income)
    {
        if (income == null) return;

        foreach (var resPair in income)
        {
            AddResource(player, resPair);
        }
    }

    /// <summary>
    /// Начисляет один конкретный ресурс с учетом лимита хранилища.
    /// </summary>
    public void AddResource(Player player, ResourcePair income)
    {
        if (!TryGetBundle(player, out var bundle))
        {
            Debug.LogWarning($"[{SystemName}] Начисление ресурса {income.Type} проигнорировано: игрок не зарегистрирован.");
            return;
        }

        var resources = bundle.Resources;

        if (!resources.ContainsKey(income.Type))
        {
            bundle.AddResources(new ResourcePair(income.Type, 0));
        }

        int oldVal = resources[income.Type];
        int limit = GetStorageLimit(player, income.Type);

        resources[income.Type] = Mathf.Min(oldVal + income.Amount, limit);
        int delta = resources[income.Type] - oldVal;

        // Рассылаем уведомление только если значение реально изменилось (не уперлось в лимит)
        if (delta != 0)
        {
            _kernel.EventBus.Raise(new ResourceChangedEvent(player, income.Type, resources[income.Type], delta, limit));
        }
    }

    public int GetResource(Player player, ResourceType type)
    {
        return TryGetBundle(player, out var bundle) ? bundle.GetResource(type) : 0;
    }

    public int GetStorageLimit(Player player, ResourceType type)
    {
        return TryGetBundle(player, out var bundle) ? bundle.GetStorageLimit(type) : 0;
    }

    /// <summary>
    /// Внутренний метод списания. Выполняется без проверок (проверки должны быть сделаны до вызова).
    /// </summary>
    private void SpendResourceInternal(Player player, ResourcePair outcome)
    {
        if (!TryGetBundle(player, out var bundle)) return;

        var resources = bundle.Resources;

        if (!resources.ContainsKey(outcome.Type)) return;

        int oldVal = resources[outcome.Type];

        resources[outcome.Type] = Mathf.Max(oldVal - outcome.Amount, 0);
        int delta = resources[outcome.Type] - oldVal;
        int limit = GetStorageLimit(player, outcome.Type);

        if (delta != 0)
        {
            _kernel.EventBus.Raise(new ResourceChangedEvent(player, outcome.Type, resources[outcome.Type], delta, limit));
        }
    }

    /// <summary>
    /// Увеличивает максимальную вместимость хранилища для ресурса.
    /// </summary>
    /// <summary>
    /// Увеличивает лимит хранения всех ресурсов игрока. Бонус главного здания фракции.
    /// </summary>
    public void IncreaseAllStorage(Player player, int amount)
    {
        if (amount <= 0) return;

        for (int i = 0; i < AllResourceTypes.Length; i++)
        {
            IncreaseStorage(player, new ResourcePair(AllResourceTypes[i], amount));
        }
    }

    /// <summary>
    /// Снимает бонус со всех складов игрока. Вызывается при уничтожении главного здания.
    /// </summary>
    public void DecreaseAllStorage(Player player, int amount)
    {
        if (amount <= 0) return;

        for (int i = 0; i < AllResourceTypes.Length; i++)
        {
            DecreaseStorage(player, new ResourcePair(AllResourceTypes[i], amount));
        }
    }

    public void IncreaseStorage(Player player, ResourcePair resourcePair)
    {
        if (!TryGetBundle(player, out var bundle))
        {
            Debug.LogWarning($"[{SystemName}] Увеличение хранилища проигнорировано: игрок не зарегистрирован.");
            return;
        }

        int newLimit = bundle.GetStorageLimit(resourcePair.Type) + resourcePair.Amount;
        bundle.SetStorageLimit(resourcePair.Type, newLimit);

        // Раньше в событие уходили нули, поэтому UI показывал 0/лимит.
        _kernel.EventBus.Raise(new ResourceChangedEvent(
            player,
            resourcePair.Type,
            bundle.GetResource(resourcePair.Type),
            0,
            newLimit));
    }

    /// <summary>
    /// Уменьшает максимальную вместимость хранилища. 
    /// Если текущее количество ресурсов превышает новый лимит, излишки сгорают.
    /// </summary>
    public void DecreaseStorage(Player player, ResourcePair resourcePair)
    {
        if (!TryGetBundle(player, out var bundle))
        {
            Debug.LogWarning($"[{SystemName}] Уменьшение хранилища проигнорировано: игрок не зарегистрирован.");
            return;
        }

        int newLimit = Mathf.Max(0, bundle.GetStorageLimit(resourcePair.Type) - resourcePair.Amount);
        bundle.SetStorageLimit(resourcePair.Type, newLimit);

        int current = bundle.GetResource(resourcePair.Type);
        if (current > newLimit)
        {
            // Излишки действительно сгорают — как и обещает документация метода.
            bundle.SetResource(resourcePair.Type, newLimit);
            _kernel.EventBus.Raise(new ResourceChangedEvent(
                player,
                resourcePair.Type,
                newLimit,
                newLimit - current,
                newLimit));
            return;
        }

        _kernel.EventBus.Raise(new ResourceChangedEvent(player, resourcePair.Type, current, 0, newLimit));
    }
}