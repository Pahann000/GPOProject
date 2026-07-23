using UnityEngine;

/// <summary>
/// Вызывается, когда загружается новый чанк карты.
/// </summary>
public struct ChunkGeneratedEvent : IGameEvent
{
    public int ChunkX;
    public int ChunkY;
    public int ChunkSize;

    public ChunkGeneratedEvent(int x, int y, int size)
    {
        ChunkX = x;
        ChunkY = y;
        ChunkSize = size;
    }
}

/// <summary>
/// Вызывается, когда игрок ставит или ломает блок (чтобы обновить 1 пиксель миникарты).
/// </summary>
public struct BlockChangedEvent : IGameEvent
{
    public int X;
    public int Y;
    public BlockType NewType;

    public BlockChangedEvent(int x, int y, BlockType type)
    {
        X = x;
        Y = y;
        NewType = type;
    }
}