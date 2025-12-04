using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Класс игрового мира.
/// </summary>
public class Map : MonoBehaviour
{
    private static Map _instance;
    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Dictionary<Vector2Int, Block> _tileData = new Dictionary<Vector2Int, Block>();

    /// <summary>
    /// Размер чанка.
    /// </summary>
    public int chunkSize { get; set; } = 16;

    /// <summary>
    /// Атлас со всеми типами блоков.
    /// </summary>
    public BlockAtlas atlas { get; set; }

    /// <summary>
    /// Материал для отображения блоков (текстуры). 
    /// </summary>
    public Material atlasMaterial { get; set; }

    /// <summary>
    /// Ширина карты.
    /// </summary>
    public int width { get; set; }

    /// <summary>
    /// Высота карты.
    /// </summary>
    public int height { get; set; }

    /// <summary>
    /// Словарь с шумами для каждого типа блоков.
    /// </summary>
    public Dictionary<Texture2D, BlockType> Noises { get; set; }

    /// <summary>
    /// Singleton-объект.
    /// </summary>
    public static Map Instance
    {
        get 
        {
            if (_instance == null)
            {
                _instance  = new Map();
            }
            return _instance;
        }
        private set {  _instance = value; }
    }


    void Awake()
    {
        Instance = this;
        transform.position = Vector3.zero;
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
        if (x > width || x < 0 || y > height || y < 0)
        {
            return new Block(new BlockData(BlockType.Air), this, x, y);
        }

        Vector2Int pos = new Vector2Int(x, y);
        if (_tileData.TryGetValue(pos, out Block data))
        {
            return data;
        }

        BlockType type = BlockType.Air;
        foreach (var (noise, blockType) in Noises)
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
    /// получает позицию чанка по мировым координатам. 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns> <see cref="Vector2Int"/> - позиция чанка </returns>
    public Vector2Int GetChunkPosition(int x, int y)
    {
        return new Vector2Int(
            Mathf.FloorToInt(x / (float)chunkSize),
            Mathf.FloorToInt(y / (float)chunkSize)
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
    /// уничтожает чанк по его координатам.
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
}