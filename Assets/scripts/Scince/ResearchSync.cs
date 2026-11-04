using Mirror;
using UnityEngine;

/// <summary>
/// Пересылает клиентам состояние древа исследований их игрока.
/// </summary>
/// <remarks>
/// Снимок отправляется при изменении версии состояния (старт/завершение
/// исследования) и периодически, пока идёт прогресс: прогресс — это время,
/// поэтому UI нужно обновлять плавно. Живёт на префабе NetworkSystems вместе
/// с остальными сетевыми системами.
/// </remarks>
public class ResearchSync : NetworkBehaviour, IGameSystem
{
    /// <summary>Как часто обновляется прогресс (секунды).</summary>
    private const float SyncInterval = 0.4f;

    public string SystemName => "Research Sync";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private float _timer;
    private int _lastVersion = int.MinValue;

    private void Start()
    {
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
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown() { }

    public void Tick(float deltaTime)
    {
        if (!isServer) return;
        if (_kernel == null || !_kernel.TryGetSystem(out ResearchSystem research)) return;

        _timer += deltaTime;

        bool versionChanged = research.Version != _lastVersion;
        if (!versionChanged && _timer < SyncInterval) return;

        _timer = 0f;
        _lastVersion = research.Version;

        SendSnapshots(research);
    }

    /// <summary>Рассылает каждому подключению состояние его игрока.</summary>
    private void SendSnapshots(ResearchSystem research)
    {
        foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
        {
            if (connection == null || connection.identity == null) continue;

            Player player = connection.identity.GetComponent<Player>();
            if (player == null) continue;
            if (!research.IsRegistered(player)) continue;

            int[] completed = research.GetCompletedIndices(player).ToArray();

            TargetApplyResearch(connection, player,
                research.GetCurrentIndex(player),
                research.GetProgress(player),
                research.GetCurrentDuration(player),
                completed);
        }
    }

    [TargetRpc]
    private void TargetApplyResearch(NetworkConnectionToClient target, Player player,
        int currentIndex, float progress, float duration, int[] completed)
    {
        ResearchClientState.Apply(player, currentIndex, progress, duration, completed);
    }
}
