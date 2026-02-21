using System.Collections.Generic;
using UnityEngine;

public class ResourceSystem : IGameSystem
{
    public string SystemName => "Resource System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;

    private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();
    private Dictionary<ResourceType, int> _storageLimits = new Dictionary<ResourceType, int>();

    // --- Реализация IGameSystem ---

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        InitializeResources();
        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {

    }

    public void FixedTick(float deltaTime) { }

    public void Shutdown()
    {
        _resources.Clear();
        _storageLimits.Clear();
    }

    // Логика системы

    private void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _resources[type] = 0;
            _storageLimits[type] = 1000;
        }

        AddResource(ResourceType.Minerals, 500);
        AddResource(ResourceType.Ice, 200);
    }

    public bool HasResources(ResourceBundle cost)
    {
        if (cost.Resources == null || cost.Resources.Count == 0) return true;

        foreach (var resPair in cost.Resources)
        {
            if (!_resources.ContainsKey(resPair.Type)) return false;
            if (_resources[resPair.Type] < resPair.Amount) return false;
        }

        return true;
    }

    public bool TrySpendResources(ResourceBundle cost)
    {
        int resourceCount = (cost.Resources != null) ? cost.Resources.Count : 0;
        Debug.Log($"[{SystemName}] TrySpendResources вызван. В корзине ресурсов: {resourceCount}");

        if (!HasResources(cost)) 
        {
            Debug.LogWarning($"[{SystemName}] Попытка списать ресурсы провалена: недостаточно средств.");
            return false; 
        }

        foreach (var resPair in cost.Resources)
        {
            SpendResourceInternal(resPair.Type, resPair.Amount);
        }

        return true;
    }

    public void AddResources(ResourceBundle income)
    {
        foreach (var resPair in income.Resources)
        {
            AddResource(resPair.Type, resPair.Amount);
        }
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (!_resources.ContainsKey(type)) _resources[type] = 0;

        int oldVal = _resources[type];
        int limit = _storageLimits.ContainsKey(type) ? _storageLimits[type] : 1000;

        _resources[type] = Mathf.Min(oldVal + amount, limit);

        int delta = _resources[type] - oldVal;

        if (delta != 0)
        {
            _kernel.EventBus.Raise(new ResourceChangedEvent(type, _resources[type], delta));
        }
    }

    public int GetResource(ResourceType type) => _resources.ContainsKey(type) ? _resources[type] : 0;

    public int GetStorageLimit(ResourceType type) => _storageLimits.ContainsKey(type) ? _storageLimits[type] : 0;

    private void SpendResourceInternal(ResourceType type, int amount)
    {
        int oldVal = _resources[type];
        _resources[type] -= amount;

        Debug.Log($"[{SystemName}] Ресурс {type} уменьшен на {amount}. Новое значение: {_resources[type]}");

        _kernel.EventBus.Raise(new ResourceChangedEvent(type, _resources[type], -amount));
    }

    public void IncreaseStorage(ResourceType type, int amount)
    {
        if (!_storageLimits.ContainsKey(type)) _storageLimits[type] = 0;
        _storageLimits[type] += amount;
    }

    public void DecreaseStorage(ResourceType type, int amount)
    {
        if (_storageLimits.ContainsKey(type))
        {
            _storageLimits[type] = Mathf.Max(0, _storageLimits[type] - amount);

            if (_resources.ContainsKey(type) && _resources[type] > _storageLimits[type])
            {
                int diff = _resources[type] - _storageLimits[type];
                SpendResourceInternal(type, diff);
            }
        }
    }
}