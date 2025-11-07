// Интерфейс для любого объекта, требующего загрузку чанков
public interface IChunkObserver
{
    public int X { get; }
    public int Y { get; }
}