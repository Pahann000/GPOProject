using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Текущие ресурсы")]
    [SerializeField] private int _minerals;
    [SerializeField] private int _energy;
    [SerializeField] private int _food;
    [SerializeField] private int _water;
    [SerializeField] private int _ice;
    [SerializeField] private int _population;
    [SerializeField] private int _maxPopulation = 10;

    [Header("Лимиты хранилищ")]
    [SerializeField] private int _maxMinerals = 100;
    [SerializeField] private int _maxEnergy = 100;
    [SerializeField] private int _maxFood = 100;
    [SerializeField] private int _maxWater = 100;
    [SerializeField] private int _maxIce = 100;

    public static event Action<ResourceType> OnResourceChanged;
    public static event Action<int> OnPopulationChanged;
    public static event Action<int> OnMaxPopulationChanged;
    public static event Action<ResourceType, int> OnStorageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public int GetResource(ResourceType type)
    {
        return type switch
        {
            ResourceType.Minerals => _minerals,
            ResourceType.Energy => _energy,
            ResourceType.Food => _food,
            ResourceType.Water => _water,
            ResourceType.Ice => _ice,
            _ => 0
        };
    }

    public int GetMaxResource(ResourceType type)
    {
        return type switch
        {
            ResourceType.Minerals => _maxMinerals,
            ResourceType.Energy => _maxEnergy,
            ResourceType.Food => _maxFood,
            ResourceType.Water => _maxWater,
            ResourceType.Ice => _maxIce,
            _ => 0
        };
    }

    public bool HasResources(ResourceBundle bundle)
    {
        foreach (var pair in bundle.Resources)
            if (GetResource(pair.Type) < pair.Amount) return false;
        return true;
    }

    public bool TrySpendResources(ResourceBundle bundle)
    {
        if (!HasResources(bundle)) return false;
        foreach (var pair in bundle.Resources)
            SpendResource(pair.Type, pair.Amount);
        return true;
    }

    public void AddResources(ResourceBundle bundle)
    {
        foreach (var pair in bundle.Resources)
            AddResource(pair.Type, pair.Amount);
    }

    public void AddResource(ResourceType type, int amount)
    {
        int current = GetResource(type);
        int max = GetMaxResource(type);
        SetResource(type, Mathf.Min(current + amount, max));
    }

    private void SpendResource(ResourceType type, int amount)
    {
        SetResource(type, GetResource(type) - amount);
    }

    private void SetResource(ResourceType type, int value)
    {
        switch (type)
        {
            case ResourceType.Minerals: _minerals = value; break;
            case ResourceType.Energy: _energy = value; break;
            case ResourceType.Food: _food = value; break;
            case ResourceType.Water: _water = value; break;
            case ResourceType.Ice: _ice = value; break;
        }
        OnResourceChanged?.Invoke(type);
    }

    public void IncreaseStorage(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Minerals: _maxMinerals += amount; break;
            case ResourceType.Energy: _maxEnergy += amount; break;
            case ResourceType.Food: _maxFood += amount; break;
            case ResourceType.Water: _maxWater += amount; break;
            case ResourceType.Ice: _maxIce += amount; break;
        }
        OnStorageChanged?.Invoke(type, GetMaxResource(type));
    }

    public void DecreaseStorage(ResourceType type, int amount)
    {
        switch (type)
        {
            case ResourceType.Minerals: _maxMinerals = Mathf.Max(0, _maxMinerals - amount); break;
            case ResourceType.Energy: _maxEnergy = Mathf.Max(0, _maxEnergy - amount); break;
            case ResourceType.Food: _maxFood = Mathf.Max(0, _maxFood - amount); break;
            case ResourceType.Water: _maxWater = Mathf.Max(0, _maxWater - amount); break;
            case ResourceType.Ice: _maxIce = Mathf.Max(0, _maxIce - amount); break;
        }
        OnStorageChanged?.Invoke(type, GetMaxResource(type));
    }

    public void IncreaseAllStorage(int amount)
    {
        _maxMinerals += amount;
        _maxEnergy += amount;
        _maxFood += amount;
        _maxWater += amount;
        _maxIce += amount;
    }

    public void DecreaseAllStorage(int amount)
    {
        _maxMinerals = Mathf.Max(0, _maxMinerals - amount);
        _maxEnergy = Mathf.Max(0, _maxEnergy - amount);
        _maxFood = Mathf.Max(0, _maxFood - amount);
        _maxWater = Mathf.Max(0, _maxWater - amount);
        _maxIce = Mathf.Max(0, _maxIce - amount);
    }

    public int Population => _population;
    public int MaxPopulation => _maxPopulation;

    public void AddPopulation(int amount)
    {
        _population = Mathf.Min(_population + amount, _maxPopulation);
        OnPopulationChanged?.Invoke(_population);
    }

    public void RemovePopulation(int amount)
    {
        _population = Mathf.Max(0, _population - amount);
        OnPopulationChanged?.Invoke(_population);
    }

    public void IncreasePopulationLimit(int amount)
    {
        _maxPopulation += amount;
        OnMaxPopulationChanged?.Invoke(_maxPopulation);
    }

    public void DecreasePopulationLimit(int amount)
    {
        _maxPopulation = Mathf.Max(0, _maxPopulation - amount);
        OnMaxPopulationChanged?.Invoke(_maxPopulation);
    }

    public int GetStorageLimit(ResourceType type)
    {
        return GetMaxResource(type);
    }
}