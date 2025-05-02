using UnityEngine;

/// <summary>
/// Класс здания-хранилища, расширяющий базовый функционал здания.
/// Предназначен для увеличения вместимости определённого типа ресурсов.
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

    /// <summary>
    /// Завершает процесс строительства здания-хранилища.
    /// После завершения строительства увеличивает доступное хранилище для указанного ресурса.
    /// </summary>
    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        ResourceManager.Instance.IncreaseStorage(StoredResource, StorageIncrease);
    }

    /// <summary>
    /// Уничтожает здание-хранилище.
    /// Перед уничтожением уменьшает доступное хранилище для указанного ресурса.
    /// </summary>
    protected override void DestroyBuilding()
    {
        ResourceManager.Instance.DecreaseStorage(StoredResource, StorageIncrease);
        base.DestroyBuilding();
    }
}