using UnityEngine;
using Mirror;

/// <summary>
/// Класс, обеспечивающий взаимодействие игрока с игровым миром.
/// </summary>
/// <remarks>
/// Фракция назначается только сервером (<see cref="FactionSystem"/>) и приходит
/// клиентам SyncVar'ом. Точка появления фракции тоже синхронизируется: люди
/// появляются на поверхности, марсиане — в пещере под землёй, поэтому позицию
/// нельзя оставлять «как в префабе» ни на сервере, ни на клиентах.
/// </remarks>
public class Player : NetworkBehaviour, IChunkObserver
{
    private bool _isRegistered = false;
    private WorldSystem _worldSystem;

    /// <summary>Фракция игрока. Назначается сервером при подключении.</summary>
    [SyncVar(hook = nameof(OnFactionChanged))]
    private FactionType _faction = FactionType.None;

    /// <summary>
    /// Точка появления уже выбрана (иначе (0,0,0) не отличить от настоящей).
    /// Объявлена раньше позиции: SyncVar'ы применяются в порядке полей, и флаг
    /// должен быть известен до хука позиции.
    /// </summary>
    [SyncVar(hook = nameof(OnFactionSpawnReadyChanged))]
    private bool _factionSpawnReady;

    /// <summary>Точка появления фракции (база). Синхронизируется клиентам.</summary>
    [SyncVar(hook = nameof(OnFactionSpawnChanged))]
    private Vector3 _factionSpawn;

    private void OnDestroy()
    {
        if (_isRegistered && _worldSystem != null && _worldSystem.WorldChunks != null)
        {
            _worldSystem.WorldChunks.UnregisterObserver(this);
        }
    }

    /// <summary>Стартовые ресурсы фракции выдаются один раз (только сервер).</summary>
    private bool _startingResourcesGranted;

    /// <summary>Снимок ресурсов отправлен клиенту (только сервер).</summary>
    private bool _resourcesSynced;

    /// <summary>
    /// Локальное хранилище ресурсов для работы UI.
    /// </summary>
    public ResourceBundle Resources { get; } = new();

    /// <summary>Фракция игрока.</summary>
    public FactionType Faction => _faction;

    /// <summary>Точка появления фракции уже известна.</summary>
    public bool FactionSpawnReady => _factionSpawnReady;

    /// <summary>
    /// Положение игрока по X.
    /// </summary>
    public int X => (int)transform.position.x;

    /// <summary>
    /// Положение игрока по Y.
    /// </summary>
    public int Y => (int)transform.position.y;

    /// <inheritdoc/>
    void Update()
    {
        // Сервер назначает фракцию сам, но порядок регистрации систем не гарантирован:
        // если FactionSystem ещё не зарегистрирован, пробуем на следующем кадре.
        if (isServer && _faction == FactionType.None)
        {
            TryAssignFaction();
            return;
        }

        // Без фракции нечего регистрировать: точка появления ещё не известна.
        if (_faction == FactionType.None) return;

        // Регистрация в экономике приходит командой клиента позже, чем фракция:
        // ресурсы выдаём, как только это стало возможно.
        if (isServer && !_startingResourcesGranted) GrantStartingResources();

        // После регистрации отправляем клиенту снимок ресурсов один раз:
        // иначе выделенный клиент показывал нули до первого изменения баланса.
        if (isServer && !_resourcesSynced) SyncResourcesToClient();

        if (_isRegistered) return;

        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        // TryGetSystem не пишет ошибку в лог: до регистрации системы её отсутствие —
        // это штатная ситуация, а не ошибка (раньше лог засорялся каждый кадр).
        if (_worldSystem == null && !kernel.TryGetSystem(out _worldSystem)) return;
        if (_worldSystem.WorldChunks == null) return;

        _worldSystem.WorldChunks.RegisterObserver(this);

        // Юнит появляется только на выбранной точке фракции, иначе он оказался бы
        // в породе (или в воздухе) в начале координат префаба.
        if (isLocalPlayer && _factionSpawnReady)
        {
            _worldSystem.CmdSpawnUnit(this);
        }

        _isRegistered = true;
        Debug.Log($"[Player] Успешно зарегистрирован в системе чанков (фракция {_faction}).");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        TryAssignFaction();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Создаем player UI
        PlayerUI playerUi = GetComponent<PlayerUI>();
        if (playerUi != null)
        {
            playerUi.Initialize(this);
        }
        else
        {
            Debug.LogWarning("[Player] На префабе игрока нет компонента PlayerUI.");
        }

        // Позицию, пришедшую в стартовом пакете, применяем сразу: камера
        // локального игрока стартует с базы фракции, а не из нуля.
        if (_factionSpawnReady) ApplySpawnPosition(_factionSpawn);

        CmdRegisterPlayer();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        // UI нужно знать локального игрока: без этого ResourcePanelUI/SelectionPanelUI
        // не могли получить его данные (LocalPlayer оставался null).
        if (GameKernel.Instance != null && GameKernel.Instance.TryGetSystem(out UISystem uiSystem))
        {
            uiSystem.InitialiseLocalPlyer(this);
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        // Player — ключ в словарях экономики и юнитов; без явной очистки
        // записи уничтоженного игрока оставались в них навсегда.
        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (kernel.TryGetSystem(out UnitSystem unitSystem)) unitSystem.UnregisterPlayer(this);
        if (kernel.TryGetSystem(out ResourceSystem resourceSystem)) resourceSystem.UnregisterPlayer(this);
        if (kernel.TryGetSystem(out ResearchSystem researchSystem)) researchSystem.UnregisterPlayer(this);

        // Освобождаем место во фракции: следующий игрок займёт её.
        if (kernel.TryGetSystem(out FactionSystem factionSystem)) factionSystem.UnregisterPlayer(this);
    }

    /// <summary>
    /// Просит сервер назначить фракцию. Пока система не готова — ничего не делает,
    /// повторный вызов безопасен.
    /// </summary>
    private void TryAssignFaction()
    {
        if (!isServer || _faction != FactionType.None) return;

        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (kernel.TryGetSystem(out FactionSystem factionSystem))
        {
            factionSystem.AssignFaction(this);
        }
    }

    /// <summary>
    /// Назначает фракцию. Вызывается только сервером: клиентам значение приходит SyncVar'ом.
    /// </summary>
    public void SetFactionServer(FactionType faction)
    {
        if (!NetworkServer.active) return;

        _faction = faction;
    }

    /// <summary>
    /// Задаёт точку появления фракции и телепортирует игрока. Вызывается только сервером.
    /// </summary>
    public void SetFactionSpawnServer(Vector3 spawnPosition)
    {
        if (!NetworkServer.active) return;

        _factionSpawn = spawnPosition;

        // Сначала позиция на сервере, затем флаг готовности: клиент применит
        // позицию, увидев true, и не «прыгнет» в ноль по ошибке.
        ApplySpawnPosition(spawnPosition);
        _factionSpawnReady = true;
    }

    private void OnFactionChanged(FactionType oldFaction, FactionType newFaction)
    {
        // На сервере позиция уже выставлена напрямую.
        if (isServer) return;

        Debug.Log($"[Player] Назначена фракция {newFaction}.");
    }

    private void OnFactionSpawnChanged(Vector3 oldPosition, Vector3 newPosition)
    {
        if (!_factionSpawnReady) return;

        ApplySpawnPosition(newPosition);
    }

    private void OnFactionSpawnReadyChanged(bool oldValue, bool newValue)
    {
        if (!newValue) return;

        ApplySpawnPosition(_factionSpawn);
    }

    private void ApplySpawnPosition(Vector3 position)
    {
        // Z оставляем прежний: у камеры-рига он задаёт плоскость мира.
        transform.position = new Vector3(position.x, position.y, transform.position.z);
    }

    [Command]
    public void CmdRegisterPlayer()
    {
        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (kernel.TryGetSystem(out UnitSystem unitSystem)) unitSystem.RegisterPlayer(this);
        if (kernel.TryGetSystem(out ResourceSystem resourceSystem)) resourceSystem.RegisterPlayer(this);
        if (kernel.TryGetSystem(out ResearchSystem researchSystem)) researchSystem.RegisterPlayer(this);

        // Стартовый запас выдаётся именно здесь: экономика уже знает игрока.
        GrantStartingResources();

        // И сразу отправляем клиенту актуальные значения: UI мог подписаться
        // на изменения позже, чем они произошли.
        SyncResourcesToClient();
    }

    /// <summary>
    /// Просит сервер начать исследование по индексу в атласе.
    /// Вызывается кнопкой в панели исследований.
    /// </summary>
    [Command]
    public void CmdStartResearch(int researchIndex)
    {
        if (!isServer) return;

        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (!kernel.TryGetSystem(out ResearchSystem researchSystem))
        {
            Debug.LogWarning("[Player] ResearchSystem недоступен: исследование не запущено.");
            return;
        }

        if (!researchSystem.TryStartResearch(this, researchIndex))
        {
            Debug.Log($"[Player] Исследование #{researchIndex} не запущено.");
        }
    }

    /// <summary>
    /// Отправляет клиенту один раз полный снимок ресурсов (количество и лимиты).
    /// </summary>
    private void SyncResourcesToClient()
    {
        if (!NetworkServer.active || _resourcesSynced) return;

        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (!kernel.TryGetSystem(out ResourceSystem resourceSystem) || !resourceSystem.IsRegistered(this)) return;
        if (!kernel.TryGetSystem(out ResourceChangedClientNotifier notifier)) return;

        notifier.SendSnapshot(this);
        _resourcesSynced = true;
    }

    /// <summary>
    /// Выдаёт фракционные стартовые ресурсы один раз (после регистрации в экономике).
    /// </summary>
    private void GrantStartingResources()
    {
        if (!isServer || _faction == FactionType.None || _startingResourcesGranted) return;

        FactionDefinition definition = FactionAtlas.GetDefinition(_faction);
        if (definition == null || definition.StartingResources == null || definition.StartingResources.Length == 0) return;

        var kernel = GameKernel.Instance;
        if (kernel == null || !kernel.TryGetSystem(out ResourceSystem resourceSystem)) return;

        // До регистрации игрока начисление невозможно: ждём следующего кадра.
        if (!resourceSystem.IsRegistered(this)) return;

        resourceSystem.AddResources(this, definition.StartingResources);
        _startingResourcesGranted = true;

        Debug.Log($"[Player] Фракция {_faction} получила стартовые ресурсы.");
    }
}
