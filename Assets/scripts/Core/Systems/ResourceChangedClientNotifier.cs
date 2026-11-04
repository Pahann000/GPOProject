using UnityEngine;
using Mirror;
using System.Collections.Generic;

/// <summary>
/// Пересылает изменения ресурсов с сервера конкретному клиенту.
/// </summary>
/// <remarks>
/// Раньше этот компонент только писал значения в <c>player.Resources</c> на клиенте,
/// но не сообщал об этом через <see cref="EventBus"/>. Из-за этого на выделенном
/// клиенте (отдельный процесс) интерфейс не обновлялся вообще: подписка UI на
/// <see cref="ResourceChangedEvent"/> срабатывала только в режиме Host, где сервер
/// и клиент — один процесс и одна шина событий.
///
/// Теперь после применения изменений событие поднимается локально, а сразу после
/// регистрации игрока сервер присылает полный снимок (количество + лимиты),
/// чтобы клиент не зависел от того, успел ли UI подписаться на дельты.
/// </remarks>
public class ResourceChangedClientNotifier : NetworkBehaviour, IGameSystem
{
    private GameKernel _kernel;
    private readonly Dictionary<Player, bool> _subscribedPlayers = new Dictionary<Player, bool>();

    public string SystemName => nameof(ResourceChangedClientNotifier);

    public bool IsActive { get; set; } = true;

    private void Start()
    {
        // Компонент живёт на NetworkSystems.prefab вместе с WorldSystem и BuilderSystem
        // и регистрируется сам, как и они. До этого он не регистрировался нигде,
        // поэтому клиент не получал ни одного ResourceChangedEvent.
        if (GameKernel.Instance == null)
        {
            Debug.LogError($"[{SystemName}] GameKernel не найден!");
            return;
        }

        GameKernel.Instance.RegisterSystem(this);
    }

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        _kernel.EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Shutdown()
    {
        if (_kernel != null)
        {
            _kernel.EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
        }

        _subscribedPlayers.Clear();
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Tick(float deltaTime) { }

    /// <summary>
    /// Серверная сторона: каждое изменение ресурсов уходит своему клиенту.
    /// Локальные (уже применённые) события и события на клиенте игнорируются:
    /// иначе в режиме Host получается бесконечный цикл «событие → RPC → событие».
    /// </summary>
    public void OnResourceChanged(ResourceChangedEvent evt)
    {
        if (evt.Local) return;
        if (!isServer) return;
        if (evt.Player == null) return;
        if (!_subscribedPlayers.ContainsKey(evt.Player)) return;

        NetworkConnectionToClient connection = evt.Player.GetComponent<NetworkIdentity>()?.connectionToClient;
        if (connection != null)
        {
            TargetNotifyPlayerResourceChanged(connection, evt);
        }
    }

    /// <summary>
    /// Отправляет клиенту полный снимок его ресурсов. Вызывается сервером после
    /// регистрации игрока в экономике: клиент получает актуальные значения и лимиты,
    /// даже если UI подписался на событие позже.
    /// </summary>
    public void SendSnapshot(Player player)
    {
        if (!isServer || player == null) return;

        NetworkConnectionToClient connection = player.GetComponent<NetworkIdentity>()?.connectionToClient;
        if (connection == null) return;

        if (_kernel == null || !_kernel.TryGetSystem(out ResourceSystem resourceSystem))
        {
            Debug.LogWarning($"[{SystemName}] ResourceSystem недоступен: снимок ресурсов не отправлен.");
            return;
        }

        var amounts = new List<ResourcePair>();
        var limits = new List<ResourcePair>();

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            amounts.Add(new ResourcePair(type, resourceSystem.GetResource(player, type)));
            limits.Add(new ResourcePair(type, resourceSystem.GetStorageLimit(player, type)));
        }

        TargetApplyResourceSnapshot(connection, player, amounts.ToArray(), limits.ToArray());
    }

    [TargetRpc]
    private void TargetNotifyPlayerResourceChanged(NetworkConnectionToClient target, ResourceChangedEvent evt)
    {
        Player player = evt.Player;
        if (player == null) return;

        player.Resources.AddResources(new ResourcePair(evt.Type, evt.Delta));

        // Лимит тоже синхронизируем, иначе UI показывал бы устаревший знаменатель.
        player.Resources.SetStorageLimit(evt.Type, evt.Limit);

        RaiseLocal(player, evt.Type, player.Resources.GetResource(evt.Type), evt.Delta, evt.Limit);
    }

    [TargetRpc]
    private void TargetApplyResourceSnapshot(NetworkConnectionToClient target, Player player, ResourcePair[] amounts, ResourcePair[] limits)
    {
        if (player == null || amounts == null) return;

        if (limits != null)
        {
            foreach (ResourcePair pair in limits)
            {
                // Сначала создаём запись ресурса: SetStorageLimit сам её добавит,
                // но так порядок одинаков для количеств и лимитов.
                player.Resources.AddResources(new ResourcePair(pair.Type, 0));
                player.Resources.SetStorageLimit(pair.Type, pair.Amount);
            }
        }

        foreach (ResourcePair pair in amounts)
        {
            player.Resources.AddResources(new ResourcePair(pair.Type, 0));

            // SetResource на отсутствующем ключе бросает KeyNotFoundException,
            // поэтому запись создаётся строкой выше.
            player.Resources.SetResource(pair.Type, pair.Amount);
        }

        foreach (ResourcePair pair in amounts)
        {
            int limit = player.Resources.GetStorageLimit(pair.Type);
            RaiseLocal(player, pair.Type, player.Resources.GetResource(pair.Type), 0, limit);
        }

        Debug.Log($"[{SystemName}] Клиент получил снимок ресурсов.");
    }

    /// <summary>
    /// Поднимает событие на локальной шине, чтобы UI обновился.
    /// Событие помечается как локальное и на сервер не пересылается.
    /// </summary>
    private void RaiseLocal(Player player, ResourceType type, int amount, int delta, int limit)
    {
        GameKernel kernel = _kernel != null ? _kernel : GameKernel.Instance;
        if (kernel == null) return;

        kernel.EventBus.Raise(ResourceChangedEvent.CreateLocal(player, type, amount, delta, limit));
    }

    /// <summary>
    /// Подписывает игрока на уведомления об изменении ресурсов.
    /// </summary>
    public void Subscribe(Player player)
    {
        if (player == null) return;

        if (!_subscribedPlayers.ContainsKey(player))
        {
            _subscribedPlayers[player] = true;
        }
    }

    /// <summary>
    /// Отписывает игрока от уведомлений об изменении ресурсов.
    /// </summary>
    public void Unsubscribe(Player player)
    {
        if (player == null) return;

        _subscribedPlayers.Remove(player);
    }
}
