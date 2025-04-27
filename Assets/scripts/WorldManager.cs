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
        StartCoroutine(InitWorld());

        Unit miner = PlaceUnit(unitAtlas.Miner, 5f, 320f);
    }

    IEnumerator InitWorld()
    {
        Seed = Random.Range(-100000, 100000);
        _worldNoise = GenerateNoiseTexture(CaveFreq, CaveSize);
        _goldNoise = GenerateNoiseTexture(GoldFrequency, GoldSize);
        CreateChunks();

        yield return GenerateTerrainAsync(); // ����������� ���������
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
                if (x % 10 == 0) yield return null; // ����� ������ 10 ������
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
        if (unitType == null)
        {
            Debug.LogError("UnitType �� �����!");
            return null;
        }

        if (unitType.Sprite == null)
        {
            Debug.LogError($"������ ��� {unitType.name} �� ��������");
            return null;
        }

        GameObject unitObj = new GameObject(unitType.name)
        {
            tag = "Unit" // ��� ��� ��������
        };

        GameObject unitsParent = GameObject.Find("Units");
        if (unitsParent == null)
        {
            Debug.LogError("������ 'Units' �� ������ � ��������!");
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
            Debug.LogWarning($"������� {spawnPos} ������, ��� ��������� ����� �����...");
            spawnPos = FindNearestFreePosition(spawnPos);
        }

        unitObj.transform.position = spawnPos;
        Debug.Log($"���� ������ �� �������: {spawnPos}");

        // ��������� ��������� ��������� (��� �������� ������)
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

    // ��������������� ����� ��� ������ ��������� �������
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
        return targetPos; // ���� �� ������, ���������� ��������
    }

    public void DestroyBlock(Block block)
    {
        Debug.Log($"���� {block.BlockType.Name} ���������");
    }
}