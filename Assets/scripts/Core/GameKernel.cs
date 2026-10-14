using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Главный контроллер игры (Сердце).
/// Инициализирует, хранит и управляет жизненным циклом всех модулей (IGameSystem).
/// Единственный разрешенный Singleton в архитектуре.
/// </summary>
public class GameKernel : NetworkBehaviour
{
    private bool _started;

    /// <summary>Провайдеры данных для сохранения подключаются один раз, когда мир уже создан.</summary>
    private bool _dataProvidersRegistered;

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
    [SerializeField] private GameObject networkSystemsPrefab;

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
    }

    private void Start()
    {
        foreach (var system in _systems)
        {
            InitSystem(system);
        }

        _started = true;
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
                system.Tick(dt);
            }
        }

        // Провайдеры данных для сохранения подключаются, когда WorldSystem уже
        // существует: он появляется из заспавненного префаба позже, чем OnStartServer.
        if (!_dataProvidersRegistered)
        {
            RegisterDataProviders();
        }
    }

    /// <summary>
    /// Регистрирует в DataCollector системах-провайдерах данных. Вызывается из Update,
    /// пока не сработает: порядок создания систем не гарантирован.
    /// </summary>
    private void RegisterDataProviders()
    {
        if (!TryGetSystem(out DataCollectorSystem collector)) return;
        if (!TryGetSystem(out WorldSystem worldSystem)) return;

        collector.RegisterProvider<SaveData>(worldSystem);
        _dataProvidersRegistered = true;

        Debug.Log("[Kernel] Провайдеры данных для сохранения зарегистрированы.");
    }

    private void FixedUpdate()
    {
        if (IsPaused) return;

        float fdt = Time.fixedDeltaTime;

        foreach (var system in _systems)
        {
            if (system.IsActive)
            {
                system.FixedTick(fdt);
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

        // Иначе остаётся «фантомная» ссылка на уничтоженный объект,
        // и следующий запуск ядра в том же процессе не сможет стать Instance.
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // Серверные системы: экономика и учёт юнитов живут только на сервере.
        RegisterSystem(new ResourceSystem());
        RegisterSystem(new UnitSystem());

        // Сохранение серверное: файл пишет только сервер, клиенты получают мир по сиду.
        RegisterSystem(new DataCollectorSystem());
        RegisterSystem(new SavingSystem());

        if (networkSystemsPrefab == null)
        {
            Debug.LogError($"[Kernel] Не назначен networkSystemsPrefab: WorldSystem, BuilderSystem и ResourceChangedClientNotifier не будут созданы!");
            return;
        }

        GameObject go = Instantiate(networkSystemsPrefab);
        NetworkServer.Spawn(go);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Клиентские системы. Раньше они не регистрировались нигде, поэтому
        // GetSystem<UISystem>() возвращал null и UI падал с NullReferenceException,
        // а выделение зданий не работало вовсе.
        RegisterSystem(new UISystem());
        RegisterSystem(new SelectionSystem());

        // Миникарта — чисто клиентская: она рисует уже синхронизированный мир.
        RegisterSystem(new MinimapSystem());

        Debug.Log("[Kernel] Клиент подключён");
    }

    private void InitSystem(IGameSystem system)
    {
        try
        {
            // Передаем Kernel и Services для инициализации
            system.Initialize(this);
            Debug.Log($"[Kernel] Система инициализирована: {system.SystemName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Kernel] Ошибка инициализации {system.SystemName}: {e}");
        }
    }

    /// <summary>
    /// Регистрирует систему в Ядре. Если ядро уже запущено, система инициализируется сразу,
    /// иначе её инициализирует Start(). Повторная регистрация того же типа игнорируется.
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

        // Если ядро уже запущено, инициализируем сразу
        if (_started)
        {
            InitSystem(system);
        }
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
    /// Пытается получить систему, не записывая ошибку в лог.
    /// Нужен для горячих путей (Update), где штатное отсутствие системы
    /// (например, до её регистрации) не является ошибкой, и для необязательных зависимостей.
    /// </summary>
    /// <typeparam name="T">Тип системы.</typeparam>
    /// <param name="system">Найденная система или null.</param>
    /// <returns>True, если система зарегистрирована.</returns>
    public bool TryGetSystem<T>(out T system) where T : class, IGameSystem
    {
        if (_systemMap.TryGetValue(typeof(T), out var found))
        {
            system = found as T;
            return system != null;
        }

        system = null;
        return false;
    }
}
