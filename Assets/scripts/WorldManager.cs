using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ����� ��� ��������� ����� ����.
/// </summary>
public class WorldManager : MonoBehaviour
{
    /// <summary>
    /// ������ ������������ ��������� ������� ������.
    /// </summary>
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

    private List<Building> _activeBuildings = new List<Building>();

    [Header("Atlases")]
    /// <summary>
    /// ����� �� ����� ������ ������.
    /// </summary>
    public BlockAtlas blockAtlas;
    /// <summary>
    /// ����� �� ����� ������ ������.
    /// </summary>
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

    [Header("Building Settings")]
    [SerializeField] private LayerMask _buildingObstacleMask;

    ///<inheritdoc/>
    void Awake() => Instance = this;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Seed = Random.Range(-100000, 100000);
        _worldNoise = GenerateNoiseTexture(CaveFreq, CaveSize);
        _goldNoise = GenerateNoiseTexture(GoldFrequency, GoldSize);
        CreateChunks();
        GenerateTerrain();
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
                if (_goldNoise.GetPixel(x,y).r > 0.5f)
                {
                    blockType = blockAtlas.Gold;
                }
                else if (_worldNoise.GetPixel(x, y).r > 0.5f)
                {
                    blockType = blockAtlas.Rock;
                }

                if (_worldNoise.GetPixel(x,y).r>0.5f)
                {
                    PlaceBlock(blockType, x, y);
                }
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

    /// <summary>
    /// ������� ���������� �����.
    /// </summary>
    /// <param name="blockType">��� �����.</param>
    /// <param name="x">x ������� ����������.</param>
    /// <param name="y">y ������� ����������</param>
    public void PlaceBlock(BlockType blockType, int x, int y)
    {
        GameObject newTile = new GameObject();

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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="unitType">��� �����.</param>
    /// <param name="x">x ������� ����������.</param>
    /// <param name="y">y ������� ����������</param>
    /// <returns>��������� ����.</returns>
    public Unit PlaceUnit(UnitType unitType, int x, int y)
    {
        //Vector2 position = new Vector2(x, y);
        //if (Physics2D.OverlapPoint(position))
        //{
        //    return null;
        //}

        GameObject newUnit = new GameObject();

        newUnit.AddComponent<SpriteRenderer>();
        newUnit.GetComponent<SpriteRenderer>().sprite = unitType.Sprite;

        newUnit.AddComponent<BoxCollider2D>();
        newUnit.GetComponent<BoxCollider2D>().size = Vector2.one;

        newUnit.AddComponent<Rigidbody2D>();
        newUnit.GetComponent<Rigidbody2D>().freezeRotation = true;

        newUnit.AddComponent<Unit>();
        newUnit.GetComponent<Unit>().unitType = unitType;

        newUnit.transform.position = new Vector3(x, y, 0);

        return newUnit.GetComponent<Unit>();
    }

    /// <summary>
    /// �������, ���������� ��� ����������� �����.
    /// </summary>
    /// <param name="block"></param>
    public void DestroyBlock(Block block)
    {
        Debug.Log($"���� {block.BlockType.Name} ���������");
    }

    /// <summary> ��������� ������ � ��������� ������� </summary>
    public Building PlaceBuilding(BuildingData data, Vector2 position)
    {
        if (!CanPlaceBuilding(data, position)) return null;
        if (!ResourceManager.Instance.TrySpendResources(data.ConstructionCost)) return null;

        GameObject buildingObj = CreateBuildingObject(data, position);
        Building building = InitializeBuildingComponent(buildingObj, data);

        if (building != null)
        {
            _activeBuildings.Add(building); // ��������� ������ � ������
        }

        return building;
    }

    private GameObject CreateBuildingObject(BuildingData data, Vector2 position)
    {
        int chunkIndex = Mathf.FloorToInt(position.x / chunkSize);
        chunkIndex = Mathf.Clamp(chunkIndex, 0, _chunks.Length - 1);

        GameObject obj = Instantiate(data.Prefab, position, Quaternion.identity);
        obj.transform.parent = _chunks[chunkIndex].transform;
        return obj;
    }

    private Building InitializeBuildingComponent(GameObject obj, BuildingData data)
    {
        Building building = obj.AddComponent<Building>();
        building.Initialize(data);

        // ��������� ���������� �� ������� ������
        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(data.Width, data.Height);
        collider.offset = new Vector2(data.Width / 2f, data.Height / 2f);

        return building;
    }

    private bool CanPlaceBuilding(BuildingData data, Vector2 position)
    {
        // �������� ��������
        Collider2D[] collisions = Physics2D.OverlapBoxAll(
            position,
            new Vector2(data.Width, data.Height),
            0,
            _buildingObstacleMask);

        if (collisions.Length > 0) return false;

        // �������� ����������� ������
        foreach (PlacementRule rule in data.PlacementRules)
        {
            if (!rule.IsSatisfied(position)) return false;
        }

        return true;
    }
}
