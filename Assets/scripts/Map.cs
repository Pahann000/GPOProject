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
    public TileAtlas atlas;
    public Material atlasMaterial;

    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Dictionary<Vector2Int, Tile> _tileData = new Dictionary<Vector2Int, Tile>();

    void Awake()
    {
        Instance = this;
        transform.position = Vector3.zero;
    }

    public void SetTile(int x, int y, TileType type)
    {
        Vector2Int pos = new Vector2Int(x, y);
        Vector2Int chunkPos = GetChunkPosition(x, y);

        if (!_chunks.TryGetValue(chunkPos, out MapChunk chunk))
        {
            chunk = CreateChunk(chunkPos);
            _chunks.Add(chunkPos, chunk);
        }

        _tileData[pos] = new Tile(new TileData(type), this, x, y);
        chunk.needsUpdate = true;
    }

    public Tile GetTile(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        return _tileData.TryGetValue(pos, out Tile data) ? data : new Tile(new TileData(TileType.Air), this, x, y);
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

    private Vector2Int GetChunkPosition(int x, int y)
    {
        return new Vector2Int(
            Mathf.FloorToInt(x / (float)chunkSize),
            Mathf.FloorToInt(y / (float)chunkSize)
        );
    }
}