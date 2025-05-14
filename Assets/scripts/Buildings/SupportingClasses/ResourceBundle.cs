using UnityEngine;
using System.Collections.Generic;

public struct ResourceBundle
{
    public Dictionary<ResourceType, int> Resources;

    public ResourceBundle(params (ResourceType type, int amount)[] resources)
    {
        Resources = new Dictionary<ResourceType, int>();
        foreach (var (type, amount) in resources)
        {
            Resources[type] = amount;
        }
    }
}