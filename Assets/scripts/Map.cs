using UnityEngine;
using System.Collections.Generic;

public class Map : MonoBehaviour
{
    private static Map _instance;
    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Dictionary<Vector2Int, Block> _tileData = new Dictionary<Vector2Int, Block>();

    // Новое поле – массив всей карты
    private BlockType[,] _worldData;

    public int chunkSize { get; set; } = 16;
    public BlockAtlas atlas { get; set; }
    public Material atlasMaterial { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public Dictionary<Texture2D, BlockType> Noises { get; set; }

    public static Map Instance
    {
        get
        {
            if (_instance == null)
                _instance = new Map();
            return _instance;
        }
        private set { _instance = value; }
    }

    void Awake()
    {
        Instance = this;
        transform.position = Vector3.zero;
    }

    /// <summary>
    /// Устанавливает готовый массив данных карты (вызывается из WorldManager после генерации).
    /// </summary>
    public void SetWorldData(BlockType[,] data)
    {
        _worldData = data;
        _tileData.Clear(); // очищаем кэш, так как теперь данные хранятся в массиве
    }

    private MapChunk CreateChunk(Vector2Int chunkPos)
    {
        GameObject chunkObj = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
        chunkObj.transform.SetParent(transform);
        chunkObj.transform.position = new Vector3(
            chunkPos.x * chunkSize,
            chunkPos.y * chunkSize,
            0
        );

        MapChunk chunk = chunkObj.AddComponent<MapChunk>();
        chunk.Init(chunkSize, atlas, atlasMaterial);
        return chunk;
    }

    public void PlaceBlock(int x, int y, BlockType type)
    {
        // Обновляем массив, если он существует
        if (_worldData != null && x >= 0 && x < width && y >= 0 && y < height)
            _worldData[x, y] = type;

        Vector2Int pos = new Vector2Int(x, y);
        _tileData[pos] = new Block(new BlockData(type), this, x, y);

        Vector2Int chunkPos = GetChunkPosition(x, y);
        GenerateChunk(chunkPos.x, chunkPos.y);
    }

    public Block GetBlockObj(int x, int y)
    {
        Block block = GetBlockInfo(x, y);
        Vector2Int pos = new Vector2Int(x, y);
        _tileData[pos] = block;
        return block;
    }

    public Block GetBlockInfo(int x, int y)
    {
        // Проверка границ
        if (x < 0 || x >= width || y < 0 || y >= height)
            return new Block(new BlockData(BlockType.Air), this, x, y);

        // Если есть массив – используем его
        if (_worldData != null)
        {
            BlockType type = _worldData[x, y];
            return new Block(new BlockData(type), this, x, y);
        }

        // Старая логика (для обратной совместимости, пока массив не задан)
        Vector2Int pos = new Vector2Int(x, y);
        if (_tileData.TryGetValue(pos, out Block data))
            return data;

        BlockType typeOld = BlockType.Air;
        foreach (var (noise, blockType) in Noises)
        {
            if (noise.GetPixel(x, y).r > 0.5f)
            {
                typeOld = blockType;
                break;
            }
        }
        return new Block(new BlockData(typeOld), this, x, y);
    }

    public Vector2Int GetChunkPosition(int x, int y)
    {
        return new Vector2Int(
            Mathf.FloorToInt(x / (float)chunkSize),
            Mathf.FloorToInt(y / (float)chunkSize)
        );
    }

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

    public void DestroyChunk(int x, int y)
    {
        Vector2Int chunkPos = new Vector2Int(x, y);
        if (_chunks.TryGetValue(chunkPos, out MapChunk chunk))
        {
            _chunks.Remove(chunkPos);
            Destroy(chunk.gameObject);
        }
    }
}