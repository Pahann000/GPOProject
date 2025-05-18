using NUnit.Framework.Constraints;
using Pathfinding;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Sprites;
using UnityEngine.UIElements;

/// <summary>
/// ����� ��� ��������� ����� ����.
/// </summary>
public class WorldManager : MonoBehaviour
{
    private AstarPath _astarPath;
    public static WorldManager Instance;
    /// <summary>
    /// ������ �������� ���� ����.
    /// </summary>
    private Texture2D _worldNoise;
    /// <summary>
    /// ������ �������� ���� ������.
    /// </summary>
    private Texture2D _goldNoise;
    /// <summary>
    /// ������ ���� ������.
    /// </summary>
    private GameObject[] _chunks;

    [Header("Atlases")]
    /// <summary>
    /// ����� �� ����� ������ ������.
    /// </summary>
    public BlockAtlas blockAtlas;

    public UnitAtlas unitAtlas;

    [Header("Noise settings")]
    /// <summary>
    /// ������ ����� � ���� (������ �������� ������ �����).
    /// </summary>
    public float CaveSize = 0.25f;
    /// <summary>
    /// ������� ��������� �����.
    /// </summary>
    public float CaveFreq = 0.05f;
    /// <summary>
    /// ������� ��������� �����������.
    /// ������ �������� - ������ �����������.
    /// </summary>
    public float TerrainFreq = 0.04f;
    /// <summary>
    /// ��������� ������ �����������.
    /// ������ �������� - ������ �����������.
    /// </summary>
    public float HighMultiplier = 25f;

    [Header("World generation")]
    /// <summary>
    /// ��� ����.
    /// </summary>
    public float Seed;
    /// <summary>
    /// ������ ������.
    /// </summary>
    public int chunkSize = 16;
    /// <summary>
    /// ������ ���� � ������.
    /// </summary>
    public int WorldWidth = 10;
    /// <summary>
    /// ������ ���� � ������.
    /// </summary>
    public int WorldHigh = 100;
    /// <summary>
    /// ������ ����������� � �������. 
    /// </summary>
    public int HighAddition = 50;

    [Header("Ores")]
    /// <summary>
    /// ������� ��������� ������.
    /// <summary/>
    public float GoldFrequency;
    /// <summary>
    /// ������ ��������� ������ (������ �������� ������ ����).
    /// </summary>
    public float GoldSize;

    void Awake() => Instance = this;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Seed = Random.Range(-100000, 100000);
        _worldNoise = GenerateNoiseTexture(CaveFreq, CaveSize);
        _goldNoise = GenerateNoiseTexture(GoldFrequency, GoldSize);
        CreateChunks();
        GenerateTerrain();
        PlaceAStarGrid(0, 0);
    }

    public void UpdateAStarGrid()
    {
        if (_astarPath != null)
        {
            _astarPath.Scan();
        }
    }

    /// <summary>
    /// ������ ������ ������.
    /// </summary>
    public void CreateChunks()
    {
        _chunks = new GameObject[WorldWidth];
        for (int i = 0; i < WorldWidth; i++)
        {
            GameObject newChunk = new GameObject();
            newChunk.name = i.ToString();
            newChunk.transform.parent = this.transform;
            _chunks[i] = newChunk;
        }
    }

    /// <summary>
    /// ���������� ����������� � ����������� ����� ���� � ����.
    /// </summary>
    private void GenerateTerrain()
    {
        for (int x = 0; x < _worldNoise.width; x++)
        {
            float height = Mathf.PerlinNoise((x + Seed) * TerrainFreq, Seed * TerrainFreq) * HighMultiplier + HighAddition;
            for (int y = 0; y < height; y++)
            {
                BlockType blockType = blockAtlas.Rock;
                if (_goldNoise.GetPixel(x, y).r > 0.5f)
                {
                    blockType = blockAtlas.Gold;
                }
                else if (_worldNoise.GetPixel(x, y).r > 0.5f)
                {
                    blockType = blockAtlas.Rock;
                }

                if (_worldNoise.GetPixel(x, y).r > 0.5f)
                {
                    PlaceBlock(blockType, x, y);
                }
            }

            if (x % chunkSize == 0)
            {
                UpdateAStarGrid();
            }
        }
    }

    /// <summary>
    /// ���������� ����� ���� �������.
    /// </summary>
    /// <param name="frequency">������� ��������� �������� �������</param>
    /// <returns>Texture2D</returns>
    private Texture2D GenerateNoiseTexture(float frequency, float limit)
    {
        Texture2D noise = new Texture2D(WorldWidth * chunkSize, WorldHigh);
        for (int x = 0; x < noise.width; x++)
        {
            for (int y = 0; y < noise.height; y++)
            {
                float v = Mathf.PerlinNoise((x + Seed) * frequency, (y + Seed) * frequency);

                if (v > limit)
                {
                    noise.SetPixel(x, y, Color.white);
                }
                else
                {
                    noise.SetPixel(x, y, Color.black);
                }
            }
        }

        noise.Apply();

        return noise;
    }

    public void PlaceBlock(BlockType blockType, int x, int y)
    {
        GameObject newTile = new GameObject();

        newTile.layer = LayerMask.NameToLayer("Terrain");

        int chunkCoord = Mathf.RoundToInt(x / chunkSize) * chunkSize;
        chunkCoord /= chunkSize;
        newTile.transform.parent = _chunks[chunkCoord].transform;

        newTile.AddComponent<Block>();
        newTile.GetComponent<Block>().BlockType = blockType;

        newTile.AddComponent<SpriteRenderer>();
        newTile.GetComponent<SpriteRenderer>().sprite = blockType.Sprite;

        newTile.AddComponent<BoxCollider2D>();
        newTile.GetComponent<BoxCollider2D>().size = Vector2.one;

        newTile.transform.position = new Vector3(x, y, 0);
    }

    public Unit PlaceUnit(UnitType unitType, float x, float y)
    {
        GameObject newUnit = new GameObject();

        newUnit.AddComponent<Unit>();
        newUnit.GetComponent<Unit>().unitType = unitType;

        newUnit.transform.position = new Vector3(x, y, 0);

        return newUnit.GetComponent<Unit>();
    }

    private void PlaceAStarGrid(float x, float y)
    {
        GameObject aStar = new GameObject("A*");
        aStar.transform.position = new Vector3(x, y, 0);

        AstarPath pathfinder = aStar.AddComponent<AstarPath>();

        GridGraph gridGraph = pathfinder.data.AddGraph(typeof(GridGraph)) as GridGraph;

        gridGraph.is2D = true;

        // ������������ �����
        gridGraph.SetDimensions(
            Mathf.CeilToInt(WorldWidth * chunkSize),
            Mathf.CeilToInt(WorldHigh * 2 / 3),
            1f
        );

        gridGraph.center = new Vector3(
            (WorldWidth * chunkSize) / 2f,
            WorldHigh * 2/3 / 2f,
            0
        );

        gridGraph.collision.use2D = true;
        gridGraph.collision.type = ColliderType.Ray;
        gridGraph.collision.mask = LayerMask.GetMask("Terrain");
        gridGraph.collision.thickRaycast = true;
        gridGraph.collision.diameter = 0.8f;

        gridGraph.maxClimb = 1;
        gridGraph.maxSlope = 90;

        // ���������
        pathfinder.Scan();

        _astarPath = pathfinder;
    }

    public void DestroyBlock(Block block)
    {
        Debug.Log($"���� {block.BlockType.Name} ���������");
    }
}