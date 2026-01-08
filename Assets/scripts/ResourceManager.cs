using UnityEngine;
using System.Collections.Generic;
using System; // ← Добавьте эту директиву для Action

public class ResourceManager : MonoBehaviour
{
    // Добавляем событие для уведомления об изменениях ресурсов
    public static event Action<ResourceType> OnResourceChanged;

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    private Dictionary<ResourceType, int> storageLimits = new Dictionary<ResourceType, int>();

    public static ResourceManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeResources();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            resources[type] = 0;
            storageLimits[type] = 1000;
        }

        // Пример стартовых ресурсов
        AddResources(new ResourceBundle(
            new ResourceBundle.ResourcePair { Type = ResourceType.Minerals, Amount = 500 },
            new ResourceBundle.ResourcePair { Type = ResourceType.Ice, Amount = 200 }
        ));
    }

    public bool HasResources(ResourceBundle cost)
    {
        // Если стоимость пустая, значит ресурсы есть
        if (cost.Resources == null || cost.Resources.Count == 0)
            return true;

        foreach (var resPair in cost.Resources)
        {
            if (!resources.ContainsKey(resPair.Type)) return false;
            if (resources[resPair.Type] < resPair.Amount) return false;
        }
        return true;
    }

    public bool TrySpendResources(ResourceBundle cost)
    {
        if (!HasResources(cost)) return false;

        foreach (var resPair in cost.Resources)
        {
            resources[resPair.Type] -= resPair.Amount;
            // Уведомляем об изменении ресурса
            OnResourceChanged?.Invoke(resPair.Type);
        }
        return true;
    }

    public void AddResources(ResourceBundle income)
    {
        foreach (var resPair in income.Resources)
        {
            if (resources.ContainsKey(resPair.Type))
            {
                int oldAmount = resources[resPair.Type];
                resources[resPair.Type] = Mathf.Min(
                    resources[resPair.Type] + resPair.Amount,
                    storageLimits[resPair.Type]
                );

                // Уведомляем об изменении, только если количество действительно изменилось
                if (oldAmount != resources[resPair.Type])
                {
                    OnResourceChanged?.Invoke(resPair.Type);
                }
            }
        }
    }

    /// <summary>
    /// Добавляет определенное количество конкретного ресурса
    /// </summary>
    public void AddResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
        {
            int oldAmount = resources[type];
            resources[type] = Mathf.Min(resources[type] + amount, storageLimits[type]);

            if (oldAmount != resources[type])
            {
                OnResourceChanged?.Invoke(type);
            }
        }
    }

    /// <summary>
    /// Тратит определенное количество конкретного ресурса
    /// </summary>
    public bool TrySpendResource(ResourceType type, int amount)
    {
        if (!resources.ContainsKey(type) || resources[type] < amount)
            return false;

        resources[type] -= amount;
        OnResourceChanged?.Invoke(type);
        return true;
    }

    public int GetResource(ResourceType type) => resources.ContainsKey(type) ? resources[type] : 0;
    public int GetStorageLimit(ResourceType type) => storageLimits.ContainsKey(type) ? storageLimits[type] : 0;

    public void IncreaseStorage(ResourceType type, int amount)
    {
        if (storageLimits.ContainsKey(type))
        {
            storageLimits[type] += amount;
            // Можно добавить событие для уведомления об изменении лимита, если нужно
            // OnStorageLimitChanged?.Invoke(type);
        }
    }

    public void DecreaseStorage(ResourceType type, int amount)
    {
        if (storageLimits.ContainsKey(type))
        {
            storageLimits[type] = Mathf.Max(0, storageLimits[type] - amount);
            // OnStorageLimitChanged?.Invoke(type);
        }
    }

    /// <summary>
    /// Устанавливает конкретное значение для ресурса (для отладки или cheat-команд)
    /// </summary>
    public void SetResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
        {
            resources[type] = Mathf.Min(amount, storageLimits[type]);
            OnResourceChanged?.Invoke(type);
        }
    }

    /// <summary>
    /// Возвращает процент заполнения хранилища для указанного ресурса
    /// </summary>
    public float GetStoragePercentage(ResourceType type)
    {
        if (!resources.ContainsKey(type) || storageLimits[type] <= 0)
            return 0f;

        return (float)resources[type] / storageLimits[type];
    }

    /// <summary>
    /// Проверяет, достигнут ли лимит хранилища для указанного ресурса
    /// </summary>
    public bool IsStorageFull(ResourceType type)
    {
        return resources.ContainsKey(type) && resources[type] >= storageLimits[type];
    }

    /// <summary>
    /// Возвращает оставшееся место в хранилище для указанного ресурса
    /// </summary>
    public int GetRemainingStorage(ResourceType type)
    {
        if (!resources.ContainsKey(type))
            return 0;

        return Mathf.Max(0, storageLimits[type] - resources[type]);
    }
}