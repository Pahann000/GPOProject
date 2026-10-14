using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Посредник между сохраняющей системой и системами-поставщиками данных:
/// они регистрируют себя здесь, а <see cref="SavingSystem"/> забирает данные,
/// не зная, кто их отдал.
/// </summary>
/// <remarks>Перенос из ветки features/13_save_system (коммит 30816d2).</remarks>
public class DataCollectorSystem : IGameSystem
{
    public string SystemName => "Data Collector";
    public bool IsActive { get; set; } = true;

    /// <summary>Провайдеры по типу данных: ключ — тип контейнера (например SaveData).</summary>
    private readonly Dictionary<Type, object> _providers = new Dictionary<Type, object>();

    public void Initialize(GameKernel kernel)
    {
        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime) { }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        _providers.Clear();
    }

    /// <summary>Регистрирует поставщика данных. Повторная регистрация того же типа заменяет прежнего.</summary>
    public void RegisterProvider<T>(IDataProvider<T> provider) where T : class
    {
        if (provider == null) return;

        _providers[typeof(T)] = provider;
        Debug.Log($"[{SystemName}] Зарегистрирован провайдер для {typeof(T).Name}.");
    }

    /// <summary>Забирает данные у провайдера нужного типа или возвращает null.</summary>
    public T GetData<T>() where T : class
    {
        if (_providers.TryGetValue(typeof(T), out object provider))
        {
            return ((IDataProvider<T>)provider).GetSaveData();
        }

        return null;
    }

    /// <summary>Отдаёт данные провайдеру нужного типа для восстановления.</summary>
    /// <returns>False, если провайдер не зарегистрирован.</returns>
    public bool RestoreData<T>(T data) where T : class
    {
        if (data == null) return false;

        if (!_providers.TryGetValue(typeof(T), out object provider))
        {
            Debug.LogWarning($"[{SystemName}] Нет провайдера для {typeof(T).Name}: восстанавливать некуда.");
            return false;
        }

        ((IDataProvider<T>)provider).RestoreFromSaveData(data);
        return true;
    }

    /// <summary>Есть ли зарегистрированный провайдер указанного типа.</summary>
    public bool HasProvider<T>() where T : class
    {
        return _providers.ContainsKey(typeof(T));
    }
}
