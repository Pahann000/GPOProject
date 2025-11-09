using UnityEngine;
using System.Collections.Generic;

public class Map : MonoBehaviour
{
    private static Map _instance;
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

    [Header("Settings")]
    public int chunkSize = 16;
    public BlockAtlas atlas;
    public Material atlasMaterial;
    public int width;
    public int height;
    public Dictionary<Texture2D, BlockType> Noises;

    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Dictionary<Vector2Int, Block> _tileData = new Dictionary<Vector2Int, Block>();

    void Awake()
    {
        Instance = this;
        transform.position = Vector3.zero;
    }

    public void PlaceBlock(int x, int y, BlockType type)
    {
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