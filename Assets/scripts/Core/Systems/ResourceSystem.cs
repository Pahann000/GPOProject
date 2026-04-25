using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// Управляет экономикой игры: хранением, начислением и списанием ресурсов.
/// Рассылает события (ResourceChangedEvent) при любом изменении баланса.
/// </summary>
public class ResourceSystem : NetworkBehaviour, IGameSystem
{
    public string SystemName => "Resource System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;

    private Dictionary<Player, ResourceBundle> _playersResources = new Dictionary<Player, ResourceBundle>();

    // TODO: В будущем можно вынести стартовые значения и лимиты в отдельный ScriptableObject (Config),
    // чтобы можно было настраивать без перекомпиляции кода.

    private void Start()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.RegisterSystem(this);
        }
        else
        {
            Debug.LogError($"[{SystemName}] GameKernel не найден!");
        }
    }

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
        //_resources.Clear();
        //_storageLimits.Clear();
    }

    /// <summary>
    /// Задает начальные значения и лимиты для всех существующих типов ресурсов.
    /// </summary>
    private void InitializePlayer(Player player)
    {
        ResourceBundle StartResourceBundle = new ResourceBundle();
        _playersResources.Add(player, StartResourceBundle);
    }

    /// <summary>
    /// Проверяет, достаточно ли ресурсов в хранилище для оплаты указанной цены.
    /// </summary>
    public bool HasResources(Player player, ResourcePair[] cost)
    {
        foreach (var resource in cost)
        {
            if (!HasResource(player, resource))
            {
                return false;
            }
        }

        return true;
    }

    public bool HasResource(Player player, ResourcePair cost)
    {
        ResourceBundle bundle = _playersResources[player];
        var resources = bundle.Resources;

        if (!resources.ContainsKey(cost.Type))
        {
            return false;
        }

        return resources[cost.Type] >= cost.Amount;
    }

    /// <summary>
    /// Пытается списать ресурсы. Если их недостаточно, операция отменяется.
    /// </summary>
    /// <returns>True, если ресурсы успешно списаны.</returns>
    public bool TrySpendResources(Player player, ResourcePair[] outcome)
    {
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
    /// Начисляет ресурсы из бандла (например, доход от здания).
    /// </summary>
    public void AddResources(Player player, ResourcePair[] income)
    {
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
        ResourceBundle bundle = _playersResources[player];
        var resources = bundle.Resources;
        var storageLimits = bundle.StorageLimits;

        if (!resources.ContainsKey(income.Type))
        {
            bundle.AddResources(new ResourcePair(income.Type, 0));
        }

        int oldVal = resources[income.Type];
        int limit = 0;

        foreach (var resource in storageLimits)
        {
            if (resource.Type == income.Type)
            {
                limit = resource.Amount;
                break;
            }
        }

        resources[income.Type] = Mathf.Min(oldVal + income.Amount, limit);
        int delta = resources[income.Type] - oldVal;

        // Рассылаем уведомление только если значение реально изменилось (не уперлось в лимит)
        if (delta != 0)
        {
            _kernel.EventBus.Raise(new ResourceChangedEvent(player, income.Type, resources[income.Type], delta));
        }
    }

    public int GetResource(Player player, ResourceType type)
    {
        if (_playersResources[player].Resources.ContainsKey(type))
        {
            return _playersResources[player].Resources[type];
        }
        return 0;
    }

    public int GetStorageLimit(Player player, ResourceType type)
    {
        foreach (ResourcePair resourcePair in _playersResources[player].StorageLimits)
        {
            if (resourcePair.Type == type)
            {
                return resourcePair.Amount;
            }
        }

        return 0;
    }

    /// <summary>
    /// Внутренний метод списания. Выполняется без проверок (проверки должны быть сделаны до вызова).
    /// </summary>
    private void SpendResourceInternal(Player player, ResourcePair outcome)
    {
        ResourceBundle bundle = _playersResources[player];
        var resources = _playersResources[player].Resources;

        resources[outcome.Type] -= outcome.Amount;
    }

    /// <summary>
    /// Увеличивает максимальную вместимость хранилища для ресурса.
    /// </summary>
    public void IncreaseStorage(Player player, ResourcePair resourcePair)
    {
        var storageLimits = _playersResources[player].StorageLimits;

        for(int i = 0; i < storageLimits.Count; i++) 
        {
            if (storageLimits[i].Type == resourcePair.Type)
            {
                storageLimits[i].Amount += resourcePair.Amount;
                return;
            }
        }
    }

    /// <summary>
    /// Уменьшает максимальную вместимость хранилища. 
    /// Если текущее количество ресурсов превышает новый лимит, излишки сгорают.
    /// </summary>
    public void DecreaseStorage(Player player, ResourcePair resourcePair)
    {
        var storageLimits = _playersResources[player].StorageLimits;

        for (int i = 0; i < storageLimits.Count; i++)
        {
            if (storageLimits[i].Type == resourcePair.Type)
            {
                storageLimits[i].Amount -= resourcePair.Amount;
                return;
            }
        }
    }
}