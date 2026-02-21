using System;
using System.Collections.Generic;
using UnityEngine;

public class EventBus
{
	private readonly Dictionary<Type, List<object>> _subscribers = new Dictionary<Type, List<object>>();

	/// <summary>
	/// Подписаться на событие.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="callback"></param>
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
	/// Отписаться от события.
	/// Не забыть вызвать при уничтожении объекта!
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="callback"></param>
	public void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
	{
		Type type = typeof(T);

		if (_subscribers.ContainsKey(type))
		{
			_subscribers[type].Remove(callback);
		}
	}

	public void Raise<T>(T eventData) where T : IGameEvent
	{
		Type type = typeof(T);

		if (_subscribers.TryGetValue(type, out List<object> callbacks))
        {
            Debug.Log($"[EventBus] Рассылка события {type.Name}. Слушателей: {callbacks.Count}");

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
		else
		{ 
            Debug.LogWarning($"[EventBus] Событие {type.Name} никто не слушает!");
        }
	}
}
