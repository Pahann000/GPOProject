using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Класс игрового мира.
/// </summary>
public class Map : MonoBehaviour
{
    private WorldConfig _config;
    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Dictionary<Vector2Int, Block> _tileData = new Dictionary<Vector2Int, Block>();
    /// <summary>
    /// Словарь с шумами для каждого типа блоков.
    /// </summary>
    private Dictionary<Texture2D, BlockType> _noises = new Dictionary<Texture2D, BlockType>();

    public int ChunkSize => _config.ChunkSize;
    public int Width => _config.WorldWidth * _config.ChunkSize;
    public int Height => _config.WorldHeight * _config.ChunkSize;

    public void Initialize(WorldConfig config)
    {
        _config = config;

        GenerateNoises();

        Debug.Log("[Map] Карта инициализирована и шумы сгенерированы");
    }

    private void GenerateNoises()
    {
        float seed = string.IsNullOrEmpty(_config.SeedString)
            ? Random.Range(-10000f, 10000f)
            : _config.SeedString.GetHashCode();

        Texture2D caveNoise = GenerateNoiseTexture(_config.CaveFreq, _config.CaveSize, seed);
        Texture2D goldNoise = GenerateNoiseTexture(_config.GoldFrequency, _config.GoldSize, seed + 100);

        _noises.Add(goldNoise, BlockType.Gold);
        _noises.Add(caveNoise, BlockType.Stone);
    }

    private Texture2D GenerateNoiseTexture(float frequency, float limit, float seed)
    {
        Texture2D noise = new Texture2D(Width, Height);
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                float xCoord = (x + seed) * frequency;
                float yCoord = (y + seed) * frequency;
                float value = Mathf.PerlinNoise(xCoord, yCoord);

                noise.SetPixel(x, y, value > limit ? Color.white : Color.black);
            }
        }

        noise.Apply();
        return noise;
    }

    private MapChunk CreateChunk(Vector2Int chunkPos)
    {
        GameObject chunkObj = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
        chunkObj.transform.SetParent(transform);
        chunkObj.transform.position = new Vector3(
            chunkPos.x * ChunkSize,
            chunkPos.y * ChunkSize,
            0
        );

        MapChunk chunk = chunkObj.AddComponent<MapChunk>();
        chunk.Init(ChunkSize, _config.BlockAtlas, _config.AtlasMaterial);
        return chunk;
    }

    /// <summary>
    /// Ставит новый блок на указанное место.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="type"> Тип блока. </param>
    public void PlaceBlock(int x, int y, BlockType type)
    {
        Vector2Int pos = new Vector2Int(x, y);
        _tileData[pos] = new Block(new BlockData(type), this, x, y);

        Vector2Int chunkPos = GetChunkPosition(x, y);
        GenerateChunk(chunkPos.x, chunkPos.y);
    }

    /// <summary>
    /// Получает объект блока, привязанного к миру
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns> <see cref="Block"/> привязанный к карте. </returns>
    public Block GetBlockObj(int x, int y)
    {
        Block block = GetBlockInfo(x, y);

        Vector2Int pos = new Vector2Int(x, y);
        _tileData[pos] = block;

        return block;
    }

    /// <summary>
    /// получает объект блока не привязанного к миру.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns> <see cref="Block"/> не привязанный к карте. </returns>
    public Block GetBlockInfo(int x, int y)
    {
        // Проверка границ
        if (x > Width || x < 0 || y > Height || y < 0)
        {
            return new Block(new BlockData(BlockType.Air), this, x, y);
        }

        Vector2Int pos = new Vector2Int(x, y);
        // Если блок уже был создан, возвращаем его
        if (_tileData.TryGetValue(pos, out Block data))
        {
            return data;
        }

        // Если блока нет в памяти вычичляем его тип по шуму
        BlockType type = BlockType.Air;
        foreach (var (noise, blockType) in _noises)
        {
            if (noise.GetPixel(x, y).r > 0.5f)
            {
                type = blockType;
                break;
            }
        }

        Block block = new Block(new BlockData(type), this, x, y);

        return block;
    }

    /// <summary>
    /// Получает позицию чанка по мировым координатам. 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns> <see cref="Vector2Int"/> - позиция чанка </returns>
    public Vector2Int GetChunkPosition(int x, int y)
    {
        return new Vector2Int(
            Mathf.FloorToInt(x / (float)ChunkSize),
            Mathf.FloorToInt(y / (float)ChunkSize)
        );
    }

    /// <summary>
    /// Генерирует объект чанка.
    /// </summary>
    /// <param name="x"> Позиция чанка по X. </param>
    /// <param name="y"> Позиция чанка по Y. </param>
    public void GenerateChunk(int x, int y)
    {
        Vector2Int chunkPos = new Vector2Int(x, y);
        if (!_chunks.TryGetValue(chunkPos, out MapChunk chunk))
        {
            chunk = CreateChunk(chunkPos);
            _chunks.Add(chunkPos, chunk);
        }

        chunk.needsUpdate = true;
    }

    /// <summary>
    /// Уничтожает чанк по его координатам.
    /// </summary>
    /// <param name="x"> Позиция чанка по X. </param>
    /// <param name="y"> Позиция чанка по Y. </param>
    public void DestroyChunk(int x, int y)
    {
        Vector2Int chunkPos = new Vector2Int(x, y);
        if (_chunks.TryGetValue(chunkPos, out MapChunk chunk))
        {
            _chunks.Remove(chunkPos);
            Destroy(chunk.gameObject);
        }
    }

    /// <summary>
    /// Очищает всю карту. 
    /// P.S. добавил на будущее.
    /// </summary>
    private void CleanUp()
    {
        foreach (var chunk in _chunks.Values)
        {
            if (chunk != null) Destroy(chunk.gameObject);
        }
        _chunks.Clear();
        _tileData.Clear();
        _noises.Clear();
    }
}