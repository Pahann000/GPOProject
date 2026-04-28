using UnityEngine;
using Mirror;

public class ResourceChangedClientNotifier : NetworkBehaviour, IGameSystem
{
    private GameKernel _kernel;

    public string SystemName => nameof(ResourceChangedClientNotifier);

    public bool IsActive { get; set; } = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.RegisterSystem(this);
        }
        else
        {
            Debug.LogError($"[{SystemName}] GameKernel не найден!");
        }
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

        NetworkConnectionToClient connection = evt.Player.gameObject.GetComponent<NetworkIdentity>().connectionToClient;
        TargetNotifyPlyerResourceChanged(connection, evt);

        Debug.Log($"сервер отправил {evt.Type} {evt.NewAmount}");
    }

    [TargetRpc]
    public void TargetNotifyPlyerResourceChanged(NetworkConnectionToClient target, ResourceChangedEvent evt)
    {
        _kernel.EventBus.Raise(evt);

        Debug.Log($"игрок получил {evt.Type} {evt.NewAmount}");
    }

    public void Shutdown(){ }

    public void FixedTick(float fixedDeltaTime){ }

    public void Tick(float deltaTime){ }

}
