using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Вспомогательный класс, позволяющмй собирать данные со всех систем через интерфейсы. Реализует паттерн «Посредник»
/// Системы регистрируют себя в коллекторе при инициализации, и он напрямую вызывает их методы, но при этом системы не знают друг о друге.
/// (в какую это папку кинуть хз)
/// </summary>
public class DataCollectorSystem : IGameSystem
{
    public string SystemName => "Data Collector";
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Соотносит данные и их тип.
    /// </summary>
    private Dictionary<Type, object> _providers = new Dictionary<Type, object>();
    private GameKernel _kernel;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        Debug.Log("[DataCollector] Инициализирован");
    }

    /// <summary>
    /// Регистрация провайдеров.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="provider"></param>
    public void RegisterProvider<T>(IDataProvider<T> provider) where T : class
    {
        _providers[typeof(T)] = provider;
        Debug.Log($"[DataCollector] Зарегистрирован провайдер для {typeof(T).Name}");
    }

    /// <summary>
    /// Возвращает текущее состояние системы в виде сериализуемого объекта.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T GetData<T>() where T : class
    {
        if (_providers.TryGetValue(typeof(T), out var provider))
            return ((IDataProvider<T>)provider).GetSaveData();
        return null;
    }

    /// <summary>
    /// Восстанавливает состояние системы из загруженных данных.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    public void RestoreData<T>(T data) where T : class
    {
        if (_providers.TryGetValue(typeof(T), out var provider))
            ((IDataProvider<T>)provider).RestoreFromSaveData(data);
    }

    public void Tick(float deltaTime) { }
    public void FixedTick(float deltaTime) { }
    public void Shutdown() { }
}