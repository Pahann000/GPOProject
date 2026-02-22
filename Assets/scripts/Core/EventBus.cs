using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Глобальная шина событий. Реализует паттерн Publisher-Subscriber.
/// Позволяет различным системам общаться друг с другом, не имея прямых ссылок друг на друга.
/// </summary>
public class EventBus
{
    // Словарь, где ключ - тип события, а значение - список методов (делегатов), которые нужно вызвать.
    private readonly Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();

    /// <summary>
    /// Подписывает метод на прослушивание определенного события.
    /// </summary>
    /// <typeparam name="T">Тип события (структура, реализующая IGameEvent).</typeparam>
    /// <param name="callback">Метод, который будет вызван при наступлении события.</param>
    public void Subscribe<T>(Action<T> callback) where T : IGameEvent
    {
        Type type = typeof(T);

        if (!_subscribers.ContainsKey(type))
        {
            _subscribers[type] = new List<object>();
        }

        _subscribers[type].Add(callback);
    }

    /// <summary>
    /// Отписывает метод от прослушивания события.
    /// ВАЖНО: Вызывать этот метод в OnDestroy, чтобы избежать утечек памяти и вызова удаленных объектов.
    /// </summary>
    /// <typeparam name="T">Тип события.</typeparam>
    /// <param name="callback">Метод, который ранее был подписан.</param>
    public void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
    {
        Type type = typeof(T);

        if (_subscribers.ContainsKey(type))
        {
            _subscribers[type].Remove(callback);
        }
    }

    /// <summary>
    /// Рассылает событие всем подписанным на него слушателям.
    /// </summary>
    /// <typeparam name="T">Тип события.</typeparam>
    /// <param name="eventData">Данные события, которые будут переданы в методы слушателей.</param>
    public void Raise<T>(T eventData) where T : IGameEvent
    {
        Type type = typeof(T);

        if (_subscribers.TryGetValue(type, out List<object> callbacks))
        {
            // Используем копию списка, чтобы избежать ошибки CollectionModified, 
            // если кто-то отпишется прямо во время выполнения события.
            foreach (var obj in new List<object>(callbacks))
            {
                var callback = obj as Action<T>;
                try
                {
                    callback?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[EventBus] Ошибка при обработке события {type.Name}: {e}");
                }
            }
        }
    }
}