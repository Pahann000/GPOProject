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
        foreach (ResourcePair r in resources)
        {
            if (Resources.ContainsKey(r.Type))
            {
                Resources[r.Type] += r.Amount;
            }
            else 
            { 
                Resources.Add(r.Type, r.Amount);
                StorageLimits.Add(new ResourcePair(r.Type, 1000));
            }
        }
    }

    public bool HasResources(ResourcePair[] cost)
    {
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

        return 0;
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