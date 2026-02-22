using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Главный контроллер игры (Сердце).
/// Инициализирует, хранит и управляет жизненным циклом всех модулей (IGameSystem).
/// Единственный разрешенный Singleton в архитектуре.
/// </summary>
public class GameKernel : MonoBehaviour
{
    /// <summary> Глобальная ссылка на Ядро. </summary>
    public static GameKernel Instance { get; private set; }

    [Header("Global Settings")]
    [Tooltip("Если true, логика систем в методах Tick и FixedTick ставится на паузу.")]
    public bool IsPaused = false;

    /// <summary> 
    /// Главная шина событий для общения систем между собой и с UI. 
    /// </summary>
    public EventBus EventBus { get; private set; }

    // Внутренние хранилища систем
    private readonly List<IGameSystem> _systems = new List<IGameSystem>();
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

        // Сборка всех модулей при запуске игры
        RegisterSystems();
    }

    private void Start()
    {
        foreach (var system in _systems)
        {
            try
            {
                system.Initialize(this);
                Debug.Log($"[Kernel] Система инициализирована: {system.SystemName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Kernel] Ошибка инициализации {system.SystemName}: {e}");
            }
        }

        // Раскомментироват чтобы выключить карту
        //GetSystem<WorldSystem>().IsActive = false;
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
                    system.FixedTick(fdt);
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

    /// <summary>
    /// Регистрирует новую систему в Ядре. Вызывается только при инициализации в Awake.
    /// </summary>
    /// <param name="system">Экземпляр системы, реализующей IGameSystem.</param>
    public void RegisterSystem(IGameSystem system)
    {
        if (_systemMap.ContainsKey(system.GetType()))
        {
            Debug.LogWarning($"[Kernel] Система {system.GetType().Name} уже зарегистрирована!");
            return;
        }

        _systems.Add(system);
        _systemMap[system.GetType()] = system;
    }

    /// <summary>
    /// Позволяет получить доступ к любой зарегистрированной системе по ее типу (Service Locator).
    /// </summary>
    /// <typeparam name="T">Тип системы (например, ResourceSystem).</typeparam>
    /// <returns>Экземпляр системы или null, если она не найдена.</returns>
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

    /// <summary>
    /// Метод-сборщик. Здесь определяется порядок запуска всех модулей игры.
    /// </summary>
    private void RegisterSystems()
    {
        Debug.Log("[Kernel] Регистрация систем...");

        RegisterSystem(new ResourceSystem());

        RegisterSystem(new UISystem());

        RegisterSystem(new WorldSystem());
        
        RegisterSystem(new BuilderSystem());
        
        RegisterSystem(new SelectionSystem());
        
        RegisterSystem(new UnitSystem());
    }
}
