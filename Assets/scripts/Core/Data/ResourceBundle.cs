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

        foreach (var r in resources)
        {
            AddResources(r);
            StorageLimits.Add(new ResourcePair(r.Type, 1000));
        }
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
            }
        }
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