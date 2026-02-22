using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет экономикой игры: хранением, начислением и списанием ресурсов.
/// Рассылает события (ResourceChangedEvent) при любом изменении баланса.
/// </summary>
public class ResourceSystem : IGameSystem
{
    public string SystemName => "Resource System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;

    // TODO: В будущем можно вынести стартовые значения и лимиты в отдельный ScriptableObject (Config),
    // чтобы можно было настраивать без перекомпиляции кода.
    private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();
    private Dictionary<ResourceType, int> _storageLimits = new Dictionary<ResourceType, int>();

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        InitializeResources();
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
        _resources.Clear();
        _storageLimits.Clear();
    }

    /// <summary>
    /// Задает начальные значения и лимиты для всех существующих типов ресурсов.
    /// </summary>
    private void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _resources[type] = 0;
            _storageLimits[type] = 1000;
        }

        // Выдача стартового капитала
        AddResource(ResourceType.Minerals, 500);
        AddResource(ResourceType.Ice, 200);
    }

    /// <summary>
    /// Проверяет, достаточно ли ресурсов в хранилище для оплаты указанной цены.
    /// </summary>
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

    /// <summary>
    /// Пытается списать ресурсы. Если их недостаточно, операция отменяется.
    /// </summary>
    /// <returns>True, если ресурсы успешно списаны.</returns>
    public bool TrySpendResources(ResourceBundle cost)
    {
        int resourceCount = (cost.Resources != null) ? cost.Resources.Count : 0;
        
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

    /// <summary>
    /// Начисляет ресурсы из бандла (например, доход от здания).
    /// </summary>
    public void AddResources(ResourceBundle income)
    {
        if (income.Resources == null) return;

        foreach (var resPair in income.Resources)
        {
            AddResource(resPair.Type, resPair.Amount);
        }
    }

    /// <summary>
    /// Начисляет один конкретный ресурс с учетом лимита хранилища.
    /// </summary>
    public void AddResource(ResourceType type, int amount)
    {
        if (!_resources.ContainsKey(type)) _resources[type] = 0;

        int oldVal = _resources[type];
        int limit = _storageLimits.ContainsKey(type) ? _storageLimits[type] : 1000;

        _resources[type] = Mathf.Min(oldVal + amount, limit);
        int delta = _resources[type] - oldVal;

        // Рассылаем уведомление только если значение реально изменилось (не уперлось в лимит)
        if (delta != 0)
        {
            _kernel.EventBus.Raise(new ResourceChangedEvent(type, _resources[type], delta));
        }
    }

    public int GetResource(ResourceType type) => _resources.ContainsKey(type) ? _resources[type] : 0;
    public int GetStorageLimit(ResourceType type) => _storageLimits.ContainsKey(type) ? _storageLimits[type] : 0;

    /// <summary>
    /// Внутренний метод списания. Выполняется без проверок (проверки должны быть сделаны до вызова).
    /// </summary>
    private void SpendResourceInternal(ResourceType type, int amount)
    {
        _resources[type] -= amount;
        _kernel.EventBus.Raise(new ResourceChangedEvent(type, _resources[type], -amount));
    }

    /// <summary>
    /// Увеличивает максимальную вместимость хранилища для ресурса.
    /// </summary>
    public void IncreaseStorage(ResourceType type, int amount)
    {
        if (!_storageLimits.ContainsKey(type)) _storageLimits[type] = 0;
        _storageLimits[type] += amount;
    }

    /// <summary>
    /// Уменьшает максимальную вместимость хранилища. 
    /// Если текущее количество ресурсов превышает новый лимит, излишки сгорают.
    /// </summary>
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