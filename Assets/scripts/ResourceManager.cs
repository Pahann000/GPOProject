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

        AddResources(new ResourceBundle(
            (ResourceType.Metal, 500),
            (ResourceType.Water, 200)
        ));
    }

    public bool HasResources(ResourceBundle cost)
    {
        foreach (var kvp in cost.Resources)
        {
            if (resources[kvp.Key] < kvp.Value)
                return false;
        }
        return true;
    }

    public bool TrySpendResources(ResourceBundle cost)
    {
        if (!HasResources(cost)) return false;

        foreach (var kvp in cost.Resources)
        {
            resources[kvp.Key] -= kvp.Value;
        }
        return true;
    }

    public void AddResources(ResourceBundle income)
    {
        foreach (var kvp in income.Resources)
        {
            resources[kvp.Key] = Mathf.Min(
                resources[kvp.Key] + kvp.Value,
                storageLimits[kvp.Key]
            );
        }
    }

    public int GetResource(ResourceType type) => resources[type];
    public int GetStorageLimit(ResourceType type) => storageLimits[type];

    public void IncreaseStorage(ResourceType type, int amount) => storageLimits[type] += amount;
    public void DecreaseStorage(ResourceType type, int amount) => storageLimits[type] -= amount;
}