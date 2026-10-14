using UnityEngine;

/// <summary>
/// Чанк карты появился (сгенерирован или загружен). Миникарта по нему
/// дорисовывает свои пиксели.
/// </summary>
public struct ChunkGeneratedEvent : IGameEvent
{
    public int ChunkX;
    public int ChunkY;
    public int ChunkSize;

    public ChunkGeneratedEvent(int chunkX, int chunkY, int chunkSize)
    {
        ChunkX = chunkX;
        ChunkY = chunkY;
        ChunkSize = chunkSize;
    }
}

/// <summary>
/// Блок поставлен или разрушен: миникарте достаточно обновить один пиксель.
/// </summary>
public struct BlockChangedEvent : IGameEvent
{
    public int X;
    public int Y;
    public BlockType NewType;

    public BlockChangedEvent(int x, int y, BlockType newType)
    {
        X = x;
        Y = y;
        NewType = newType;
    }
}
