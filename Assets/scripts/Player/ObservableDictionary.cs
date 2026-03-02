using System.Collections.Generic;
using System;

/// <summary>
/// Класс, описывающий словарь с системой событий.
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ObservableDictionary<TKey, TValue> : Dictionary<TKey, TValue>
{
    /// <summary>
    /// Событие изменения объекта в словаре.
    /// </summary>
    public event Action<TKey> ItemCahnged;

    /// <summary>
    /// Добавляет новый объект в словарь.
    /// </summary>
    /// <param name="key"> Ключ. </param>
    /// <param name="value"> Значение. </param>
    public new void Add(TKey key, TValue value)
    {
        if (!this.ContainsKey(key))
        {
            base.Add(key, value);
        }
        else
        {
            return;
        }

        ItemCahnged?.Invoke(key);
    }

    /// <summary>
    /// Удаляет элемент словаря.
    /// </summary>
    /// <param name="key"> Ключ. </param>
    /// <returns>
    /// true - если удаление успешно,
    /// иначе - false.
    /// </returns>
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