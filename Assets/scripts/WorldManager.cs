using NUnit.Framework.Constraints;
using Pathfinding;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Sprites;
using UnityEngine.UIElements;

/// <summary>
/// Класс для генерации карты мира.
/// </summary>
public class WorldManager : MonoBehaviour
{
    private AstarPath _astarPath;
    public static WorldManager Instance;
    /// <summary>
    /// Хранит текстуру шума мира.
    /// </summary>
    private Texture2D _worldNoise;
    /// <summary>
    /// Хранит текстуру шума золота.
    /// </summary>
    private Texture2D _goldNoise;
    /// <summary>
    /// Список всех чанков.
    /// </summary>
    private GameObject[] _chunks;

    [Header("Atlases")]
    /// <summary>
    /// Атлас со всеми видами блоков.
    /// </summary>
    public BlockAtlas blockAtlas;

    public UnitAtlas unitAtlas;

    [Header("Noise settings")]
    /// <summary>
    /// Размер пещер в мире (больше значение меньше пещер).
    /// </summary>
    public float CaveSize = 0.25f;
    /// <summary>
    /// Частота появления пещер.
    /// </summary>
    public float CaveFreq = 0.05f;
    /// <summary>
    /// Частота появления поверхности.
    /// Больше значения - больше поверхности.
    /// </summary>
    public float TerrainFreq = 0.04f;
    /// <summary>
    /// Множитель высоты поверхности.
    /// Меньше значение - ровнее поверхность.
    /// </summary>
    public float HighMultiplier = 25f;

    [Header("World generation")]
    /// <summary>
    /// Сид мира.
    /// </summary>
    public float Seed;
    /// <summary>
    /// Размер чанков.
    /// </summary>
    public int chunkSize = 16;
    /// <summary>
    /// Ширина мира в чанках.
    /// </summary>
    public int WorldWidth = 10;
    /// <summary>
    /// Высота мира в блоках.
    /// </summary>
    public int WorldHigh = 100;
    /// <summary>
    /// Высота поверхности в блоаках. 
    /// </summary>
    public int HighAddition = 50;

    [Header("Ores")]
    /// <summary>
    /// Частота появления золота.
    /// <summary/>
    public float GoldFrequency;
    /// <summary>
    /// Размер кластеров золота (больше значение меньше руды).
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
    /// Создаёт список чанков.
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
    /// генерирует поверхность и выстраивает карту мира в игре.
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
    /// Генерирует карту шума перлина.
    /// </summary>
    /// <param name="frequency">Частота появления активной области</param>
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

        // Конфигурация графа
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

        // Сканируем
        pathfinder.Scan();

        _astarPath = pathfinder;
    }

    public void DestroyBlock(Block block)
    {
        Debug.Log($"блок {block.BlockType.Name} уничтожен");
    }
}