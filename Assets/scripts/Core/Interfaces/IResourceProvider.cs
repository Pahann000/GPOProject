using System;

/// <summary>
/// Интерфейс для работы с ресурсами.
/// </summary>
public interface IResourceProvider
{
    /// <summary>
    /// Проверяет, достаточно ли ресурсов для оплаты указанной цены.
    /// </summary>
    bool HasResources(Player player, ResourcePair[] cost);

    /// <summary>
    /// Проверяет наличие конкретного ресурса.
    /// </summary>
    bool HasResource(Player player, ResourcePair cost);

    /// <summary>
    /// Пытается списать ресурсы.
    /// </summary>
    bool TrySpendResources(Player player, ResourcePair[] outcome);

    /// <summary>
    /// Начисляет ресурсы.
    /// </summary>
    void AddResources(Player player, ResourcePair[] income);

    /// <summary>
    /// Начисляет один ресурс.
    /// </summary>
    void AddResource(Player player, ResourcePair income);

    /// <summary>
    /// Получает количество ресурса.
    /// </summary>
    int GetResource(Player player, ResourceType type);

    /// <summary>
    /// Получает лимит хранилища.
    /// </summary>
    int GetStorageLimit(Player player, ResourceType type);
}
