using System.Collections.Generic;

[System.Serializable]
public class ResourcePair
{
    public ResourceType Type;
    public int Amount;

    public ResourcePair(ResourceType type, int amount)
    {
        this.Type = type;
        this.Amount = amount;
    }

    // Обязательный конструктор по умолчанию для Mirror
    public ResourcePair()
    {
        Type = default; // или ResourceType.None, если определено значение по умолчанию
        Amount = 0;
    }

}

[System.Serializable]
public class ResourceBundle
{
    /// <summary>
    /// Лимит по умолчанию для типа ресурса, для которого запись ещё не создана.
    /// TODO: заменить на значение из конфига баланса, когда он будет подключён.
    /// </summary>
    public const int DefaultStorageLimit = 1000;

    public Dictionary<ResourceType, int> Resources;
    public List<ResourcePair> StorageLimits;

    public ResourceBundle(params ResourcePair[] resources)
    {
        Resources = new Dictionary<ResourceType, int>();
        StorageLimits = new List<ResourcePair>();

        AddResources(resources);
    }

    public ResourceBundle()
    {
        Resources = new Dictionary<ResourceType, int>();
        StorageLimits = new List<ResourcePair>();
    }

    public void AddResources(params ResourcePair[] resources)
    {
        if (resources == null) return;

        foreach (ResourcePair r in resources)
        {
            if (Resources.ContainsKey(r.Type))
            {
                Resources[r.Type] += r.Amount;
            }
            else 
            { 
                Resources.Add(r.Type, r.Amount);
                StorageLimits.Add(new ResourcePair(r.Type, DefaultStorageLimit));
            }
        }
    }

    public bool HasResources(ResourcePair[] cost)
    {
        // Пустая или отсутствующая цена — постройка/производство бесплатны,
        // а не NullReferenceException.
        if (cost == null || cost.Length == 0) return true;

        foreach (var resource in cost)
        {
            if (!HasResource(resource))
            {
                return false;
            }
        }

        return true;
    }

    public bool HasResource(ResourcePair cost)
    {
        if (!Resources.ContainsKey(cost.Type))
        {
            return false;
        }

        return Resources[cost.Type] >= cost.Amount;
    }

    public int GetResource(ResourceType type)
    {
        if (Resources.ContainsKey(type))
        {
            return Resources[type];
        }
        return 0;
    }

    public int GetStorageLimit(ResourceType type)
    {
        foreach (ResourcePair resourcePair in StorageLimits)
        {
            if (resourcePair.Type == type)
            {
                return resourcePair.Amount;
            }
        }

        // Раньше здесь возвращался 0, и для типа без записи лимита
        // Mathf.Min(old + income, 0) навсегда обнулял начисление.
        return DefaultStorageLimit;
    }

    /// <summary>
    /// Задает лимит хранилища для ресурса, создавая запись при необходимости.
    /// </summary>
    public void SetStorageLimit(ResourceType type, int limit)
    {
        if (limit < 0) limit = 0;

        for (int i = 0; i < StorageLimits.Count; i++)
        {
            if (StorageLimits[i].Type == type)
            {
                StorageLimits[i].Amount = limit;
                return;
            }
        }

        StorageLimits.Add(new ResourcePair(type, limit));
    }

    /// <summary>
    /// Задает точное количество ресурса (например, при срезе по новому лимиту).
    /// </summary>
    public void SetResource(ResourceType type, int amount)
    {
        Resources[type] = amount < 0 ? 0 : amount;
    }

    public static ResourceBundle Create(params (ResourceType type, int amount)[] resources)
    {
        var bundle = new ResourceBundle();

        foreach (var (type, amount) in resources)
        {
            bundle.AddResources(new ResourcePair(type, amount));
        }

        return bundle;
    }
}