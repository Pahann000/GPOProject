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
        StartCoroutine(InitWorld());

        Unit miner = PlaceUnit(unitAtlas.Miner, 5f, 320f);
    }

    IEnumerator InitWorld()
    {
        Seed = Random.Range(-100000, 100000);
        _worldNoise = GenerateNoiseTexture(CaveFreq, CaveSize);
        _goldNoise = GenerateNoiseTexture(GoldFrequency, GoldSize);
        CreateChunks();

        yield return GenerateTerrainAsync(); // Постепенная генерация
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
    IEnumerator GenerateTerrainAsync()
    {
        for (int x = 0; x < _worldNoise.width; x++)
        {
            float height = Mathf.PerlinNoise((x + Seed) * TerrainFreq, Seed * TerrainFreq) * HighMultiplier + HighAddition;
            for (int y = 0; y < height; y++)
            {
                if (_worldNoise.GetPixel(x, y).r > 0.5f)
                {
                    PlaceBlock(blockAtlas.Rock, x, y);
                }
                if (x % 10 == 0) yield return null; // Пауза каждые 10 блоков
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
        if (unitType == null)
        {
            Debug.LogError("UnitType не задан!");
            return null;
        }

        if (unitType.Sprite == null)
        {
            Debug.LogError($"Спрайт для {unitType.name} не назначен");
            return null;
        }

        GameObject unitObj = new GameObject(unitType.name)
        {
            tag = "Unit" // тег для удобства
        };

        GameObject unitsParent = GameObject.Find("Units");
        if (unitsParent == null)
        {
            Debug.LogError("Объект 'Units' не найден в иерархии!");
            return null;
        }

        unitObj.transform.parent = unitsParent.transform;

        SpriteRenderer renderer = unitObj.AddComponent<SpriteRenderer>();
        renderer.sprite = unitType.Sprite;
        renderer.sortingLayerName = "Units";
        renderer.sortingOrder = 1;

        CircleCollider2D collider = unitObj.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        Rigidbody2D rb = unitObj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2f;
        rb.freezeRotation = true;

        Unit unit = unitObj.AddComponent<Unit>();
        unit.unitType = unitType;

        unitObj.AddComponent<Seeker>();
        AIPath aiPath = unitObj.AddComponent<AIPath>();
        aiPath.radius = 1f;
        aiPath.maxSpeed = 3f;
        aiPath.orientation = OrientationMode.YAxisForward;
        aiPath.pickNextWaypointDist = 1.2f;
        unitObj.AddComponent<AIDestinationSetter>();

        Vector3 spawnPos = new Vector3(x, y, 0);
        if (Physics2D.OverlapPoint(spawnPos))
        {
            Debug.LogWarning($"Позиция {spawnPos} занята, ищу свободное место рядом...");
            spawnPos = FindNearestFreePosition(spawnPos);
        }

        unitObj.transform.position = spawnPos;
        Debug.Log($"Юнит создан на позиции: {spawnPos}");

        // Добавляем индикатор выделения (как дочерний объект)
        GameObject indicator = new GameObject("SelectionIndicator");
        indicator.transform.parent = unitObj.transform;
        indicator.transform.localPosition = Vector3.zero;

        SpriteRenderer indicatorRenderer = indicator.AddComponent<SpriteRenderer>();
        indicatorRenderer.sprite = unitType.Sprite;
        indicatorRenderer.color = Color.green;
        indicatorRenderer.sortingOrder = 10;
        indicator.SetActive(false);

        unit.selectionIndicator = indicator;

        return unit;
    }

    // Вспомогательный метод для поиска свободной позиции
    private Vector3 FindNearestFreePosition(Vector3 targetPos)
    {
        for (int i = 1; i < 5; i++)
        {
            Vector3[] directions = {
            targetPos + Vector3.up * i,
            targetPos + Vector3.down * i,
            targetPos + Vector3.left * i,
            targetPos + Vector3.right * i
        };

            foreach (Vector3 pos in directions)
            {
                if (!Physics2D.OverlapPoint(pos))
                    return pos;
            }
        }
        return targetPos; // Если всё занято, возвращаем исходную
    }

    public void DestroyBlock(Block block)
    {
        Debug.Log($"блок {block.BlockType.Name} уничтожен");
    }
}