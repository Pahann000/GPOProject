using System.Collections.Generic;
using System;

public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    public event Action<TKey> ItemCahnged;

    public new void Add(TKey key, TValue value)
    {
        try
        {
            base.Add(key, value);
        }
        catch
        {
            return;
        }
        ItemCahnged?.Invoke(key);
    }

    public new bool Remove(TKey key)
    {
        if (base.Remove(key))
        {
            ItemCahnged?.Invoke(key);
            return true;
        }
        return false;
    }

    public new TValue this[TKey key]
    {
        get => base[key];
        set
        {
            base[key] = value;
            ItemCahnged?.Invoke(key);
        }
    }
}