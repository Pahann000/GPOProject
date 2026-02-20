using System;
using System.Collections.Generic;
using UnityEngine;

public class GameKernel : MonoBehaviour
{
    public static GameKernel Instance { get; private set; }

    [Header("Global Settings")]
    public bool IsPaused = false;

    // Шина событий
    public EventBus EventBus { get; private set; }

    // Список всех подключнных систем
    private readonly List<IGameSystem> _system = new List<IGameSystem>();

    // Словарь для быстрого поиска систем по типу
    private readonly Dictionary<Type, IGameSystem> _systemMap = new Dictionary<Type, IGameSystem>();

    private void Awake()
    {
        // Паттерн Singleton для Ядра
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EventBus = new EventBus();

        RegisterSystems();
    }

    private void Start()
    {
        foreach (var system in _systems)
        {
            try
            {
                system.Initialize(this);
                Debug.Log($"[Kernel] Система инициализирована: {system.SystemName}";
            }
            catch (Exception e)
            {
                Debug.LogError($"[Kernel] Ошибка инициализации {system.SystemName}: {e}");
            }
        }
    }

    private void Update()
    {
        if (IsPaused) return;

        float dt = Time.deltaTime;

        foreach (var system in _systems)
        {
            if (system.IsActive)
            {
                try
                {
                    system.Tick(dt);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Kernel] Ошибка в Tick системы {system.SystemName}: {e}");
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (IsPaused) return;

        float fdt = Time.fixedDeltaTime;

        foreach (var system in _systems)
        {
            if (system.IsActive)
            {
                try
                {
                    systam.FixedTick(fdt);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Kernel] Ошибка в FixedTick системы {system.SystemName}: {e}");
                }
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var system in _systems)
        {
            system.Shutdown();
        }

        _systems.Clear();
        _systemMap.Clear();
    }

    // --- Управление системами ---

    public void RegisterSystem(IGameSystem system)
    {
        if (_systemMap.ContainsKey(system.GetType()))
        {
            Debug.Log($"[Kernel] Система {system.GetType().Name} уже зарегистрирована!");
            return;
        }

        _systems.Add(system);
        _systemMap[system.GetType()] = system;
    }

    public T GetSystem<T>() where T : class, IGameSystem
    {
        var type = typeof(T);
        if (_systemMap.TryGetValue(type, out var system))
        {
            return system as T;
        }

        Debug.LogError($"[Kernel] Запрошенная система не найдена: {type.Name}");
        return null;
    }

    private void RegisterSystems()
    {
        Debug.Log("[Kernel] Регистрация систем...");

        // Позже добавить все системы.
    }
}
