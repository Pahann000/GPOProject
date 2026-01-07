/// <summary>
/// Интерфейс для любого объекта, требующего загрузку чанков.
/// </summary>
public interface IChunkObserver
{
    /// <summary>
    /// Позиция объекта по X.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Позиция объекта по Y.
    /// </summary>
    public int Y { get; }
}