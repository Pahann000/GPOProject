using UnityEngine;
using Mirror;

/// <summary>
/// Класс, обеспечивающий взаимодействие игрока с игровым миром.
/// </summary>
public class Player : NetworkBehaviour, IChunkObserver
{
    private bool _isRegistered = false;
    private WorldSystem _worldSystem;

    private void OnDestroy()
    {
        if (_isRegistered && _worldSystem != null && _worldSystem.WorldChunks != null)
        {
            _worldSystem.WorldChunks.UnregisterObserver(this);
        }
    }

    /// <summary>
    /// Локальное хранилище ресурсов для работы UI.
    /// </summary>
    public ResourceBundle Resources { get; } = new();

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
        if (_isRegistered) return;

        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        // TryGetSystem не пишет ошибку в лог: до регистрации системы её отсутствие —
        // это штатная ситуация, а не ошибка (раньше лог засорялся каждый кадр).
        if (_worldSystem == null && !kernel.TryGetSystem(out _worldSystem)) return;
        if (_worldSystem.WorldChunks == null) return;

        _worldSystem.WorldChunks.RegisterObserver(this);

        if (isLocalPlayer)
        {
            _worldSystem.CmdSpawnUnit(this);
        }

        _isRegistered = true;
        Debug.Log("[Player] Успешно зарегистрирован в системе чанков.");
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
    }

    [Command]
    public void CmdRegisterPlayer()
    {
        var kernel = GameKernel.Instance;
        if (kernel == null) return;

        if (kernel.TryGetSystem(out UnitSystem unitSystem)) unitSystem.RegisterPlayer(this);
        if (kernel.TryGetSystem(out ResourceSystem resourceSystem)) resourceSystem.RegisterPlayer(this);
    }
}
