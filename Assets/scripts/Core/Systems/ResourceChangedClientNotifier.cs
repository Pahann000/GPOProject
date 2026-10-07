using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class ResourceChangedClientNotifier : NetworkBehaviour, IGameSystem
{
    private GameKernel _kernel;
    private Dictionary<Player, bool> _subscribedPlayers = new Dictionary<Player, bool>();

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

    [Server]
    public void OnResourceChanged(ResourceChangedEvent evt)
    {
        if (evt.Player == null) return;

        var connection = evt.Player.GetComponent<NetworkIdentity>()?.connectionToClient;
        if (connection != null)
        {
            TargetNotifyPlyerResourceChanged(connection, evt);
        }
    }

    [TargetRpc]
    public void TargetNotifyPlyerResourceChanged(NetworkConnectionToClient target, ResourceChangedEvent evt)
    {
        // Получаем локальный ресурс-менеджер игрока
        var player = evt.Player;
        if (player != null)
        {
            player.Resources.AddResources(new ResourcePair(evt.Type, evt.Delta));
            // Лимит тоже синхронизируем, иначе UI показывал бы устаревший знаменатель.
            player.Resources.SetStorageLimit(evt.Type, evt.Limit);
        }
    }

    public void Shutdown()
    {
        _kernel.EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
        _subscribedPlayers.Clear();
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Tick(float deltaTime) { }

    /// <summary>
    /// Подписывает игрока на уведомления об изменении ресурсов.
    /// </summary>
    public void Subscribe(Player player)
    {
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
        _subscribedPlayers.Remove(player);
    }
}
