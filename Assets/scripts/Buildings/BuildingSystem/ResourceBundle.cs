using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ResourceBundle
{
    [System.Serializable]
    public struct ResourcePair
    {
        public ResourceType Type;
        public int Amount;
    }

    public List<ResourcePair> Resources;

    public ResourceBundle(params ResourcePair[] resources)
    {
        Resources = new List<ResourcePair>();
        foreach (var r in resources)
        {
            Resources.Add(r);
        }
    }

    public static ResourceBundle Create(params (ResourceType type, int amount)[] resources)
    {
        var bundle = new ResourceBundle();
        bundle.Resources = new List<ResourcePair>();

        foreach (var (type, amount) in resources)
        {
            bundle.Resources.Add(new ResourcePair
            {
                Type = type,
                Amount = amount
            });
        }

        return bundle;
    }
}