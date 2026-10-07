using UnityEngine;

/// <summary>
/// Здание-хранилище: увеличивает вместимость указанного ресурса, пока существует,
/// и возвращает лимит обратно при уничтожении.
/// Раньше лимит только уменьшался (увеличение было в закомментированном методе
/// с несуществующим API), поэтому хранилище работало «в минус».
/// </summary>
public class StorageBuilding : Building
{
    /// <summary>
    /// Тип ресурса, который хранит это здание.
    /// </summary>
    public ResourceType StoredResource;

    /// <summary>
    /// Величина увеличения вместимости хранилища для указанного ресурса.
    /// </summary>
    public int StorageIncrease;

    protected override void Start()
    {
        base.Start();

        if (!isServer || Owner == null) return;

        if (GameKernel.Instance != null && GameKernel.Instance.TryGetSystem(out ResourceSystem resourceSystem))
        {
            resourceSystem.IncreaseStorage(Owner, new ResourcePair(StoredResource, StorageIncrease));
        }
    }

    /// <summary>
    /// Уничтожает здание-хранилище.
    /// Перед уничтожением уменьшает доступное хранилище для указанного ресурса.
    /// </summary>
    protected override void DestroyBuilding()
    {
        if (!isServer) return;

        if (Owner != null && GameKernel.Instance != null &&
            GameKernel.Instance.TryGetSystem(out ResourceSystem resourceSystem))
        {
            resourceSystem.DecreaseStorage(Owner, new ResourcePair(StoredResource, StorageIncrease));
        }

        base.DestroyBuilding();
    }
}
