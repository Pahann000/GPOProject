using UnityEngine;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
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
            new ResourceBundle.ResourcePair { Type = ResourceType.Metal, Amount = 500 },
            new ResourceBundle.ResourcePair { Type = ResourceType.Water, Amount = 200 }
        ));
    }

    public bool HasResources(ResourceBundle cost)
    {
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
        }
        return true;
    }

    public void AddResources(ResourceBundle income)
    {
        foreach (var resPair in income.Resources)
        {
            if (resources.ContainsKey(resPair.Type))
            {
                resources[resPair.Type] = Mathf.Min(
                    resources[resPair.Type] + resPair.Amount,
                    storageLimits[resPair.Type]
                );
            }
        }
    }

    public int GetResource(ResourceType type) => resources.ContainsKey(type) ? resources[type] : 0;
    public int GetStorageLimit(ResourceType type) => storageLimits.ContainsKey(type) ? storageLimits[type] : 0;

    public void IncreaseStorage(ResourceType type, int amount)
    {
        if (storageLimits.ContainsKey(type)) storageLimits[type] += amount;
    }

    public void DecreaseStorage(ResourceType type, int amount)
    {
        if (storageLimits.ContainsKey(type)) storageLimits[type] = Mathf.Max(0, storageLimits[type] - amount);
    }
}