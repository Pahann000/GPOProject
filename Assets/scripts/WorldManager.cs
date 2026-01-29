using System.Collections.Generic;
using UnityEngine;

// в пизду, сами пишите комментарии здесь

/// <summary>
/// ����� ��� ��������� ����� ����.
/// </summary>
public class WorldManager : MonoBehaviour
{
    [SerializeField]private BlockAtlas atlas;
    [SerializeField]private Material atlasMaterial;
    [SerializeField] private LayerMask _buildingObstacleMask;
    [SerializeField] private List<BlockType> _buildableBlockTypes = new List<BlockType>();
    [SerializeField] private List<BlockType> _resourceBlockTypes = new List<BlockType>();

    /// <summary>
    /// 
    /// </summary>
    private List<Building> _activeBuildings = new List<Building>();
    /// <summary>
    /// ������ �������� ���� ����.
    /// </summary>
    private Texture2D _worldNoise;
    /// <summary>
    /// ������ �������� ���� ������.
    /// </summary>
    private Texture2D _goldNoise;

    private Dictionary<Texture2D, BlockType> Noises = new();

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
    public int WorldWidth = 100;
    /// <summary>
    /// ������ ���� � ������.
    /// </summary>
    public int WorldHeight = 100;
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



    void Awake()
    {
        InitializeBuildableBlocks();
    }

    private void InitializeBuildableBlocks()
    {
        // Àâòîìàòè÷åñêè çàïîëíÿåì ñïèñîê ïðèãîäíûõ áëîêîâ
        if (_buildableBlockTypes.Count == 0)
        {
            _buildableBlockTypes.Add(BlockType.Stone);
            Debug.Log($"Äîáàâëåí ïðèãîäíûé áëîê: {BlockType.Stone.ToString()}");
        }

        // Àâòîìàòè÷åñêè çàïîëíÿåì ñïèñîê ðåñóðñíûõ áëîêîâ
        if (_resourceBlockTypes.Count == 0)
        {
            _resourceBlockTypes.Add(BlockType.Minerals);
            Debug.Log($"Äîáàâëåí ðåñóðñíûé áëîê: {BlockType.Minerals.ToString()}");

            _resourceBlockTypes.Add(BlockType.Root);
            Debug.Log($"Äîáàâëåí ðåñóðñíûé áëîê: {BlockType.Root.ToString()}");

            _resourceBlockTypes.Add(BlockType.Ice);
            Debug.Log($"Äîáàâëåí ðåñóðñíûé áëîê: {BlockType.Ice.ToString()}");
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateMapObject();

        Seed = Random.Range(-100000, 100000);
        _worldNoise = GenerateNoiseTexture(CaveFreq, CaveSize);
        _goldNoise = GenerateNoiseTexture(GoldFrequency, GoldSize);

        Noises.Add(_goldNoise, BlockType.Gold);
        Noises.Add(_worldNoise, BlockType.Stone);

        GenerateChunkManager();
    }

    private Map GenerateMapObject()
    {
        GameObject MapGameObject = new GameObject("Map");
        Map map = MapGameObject.AddComponent<Map>();
        map.atlas = atlas;
        map.atlasMaterial = atlasMaterial;
        map.Noises = Noises;
        map.width = WorldWidth * chunkSize;
        map.height = WorldHeight * chunkSize;
        return map;
    }

    private ChunkManager GenerateChunkManager()
    {
        GameObject chunkManagerGameObject = new GameObject("ChunkManager");
        ChunkManager chunkManager = chunkManagerGameObject.AddComponent<ChunkManager>();
        return chunkManager;
    }

    /// <summary>
    /// ���������� ����� ���� �������.
    /// </summary>
    /// <param name="frequency">������� ��������� �������� �������</param>
    /// <returns>Texture2D</returns>
    private Texture2D GenerateNoiseTexture(float frequency, float limit)
    {
        Texture2D noise = new Texture2D(WorldWidth * chunkSize, WorldHeight * chunkSize);
        for (int x = 0; x < noise.width; x++)
        {
            for (int y = 0; y < noise.height; y++)
            {
                // ���������� ������ �������� ��� ������ �������
                float xCoord = (x + Seed) * frequency;
                float yCoord = (y + Seed) * frequency;

                float value = Mathf.PerlinNoise(xCoord, yCoord);

                // Normalize �������� ��� ������ ���������
                noise.SetPixel(x, y, value > limit ? Color.white : Color.black);
            }
        }

        noise.Apply();
        return noise;
    }

    public Building PlaceBuilding(BuildingData data, Vector2 position)
    {
        Debug.Log($"WorldManager: Ïîïûòêà ðàçìåñòèòü {data.DisplayName} íà {position}");

        if (!CanPlaceBuilding(data, position))
        {
            Debug.Log("WorldManager: Íåëüçÿ ðàçìåñòèòü çäàíèå (íå ïðîéäåíû ïðîâåðêè)");
            return null;
        }

        if (!ResourceManager.Instance.TrySpendResources(data.ConstructionCost))
        {
            Debug.Log("WorldManager: Íå óäàëîñü ñïèñàòü ðåñóðñû");
            return null;
        }

        GameObject buildingObj = CreateBuildingObject(data, position);
        Building building = InitializeBuildingComponent(buildingObj, data);

        if (building != null)
        {
            _activeBuildings.Add(building);
            Debug.Log($"WorldManager: Çäàíèå {data.DisplayName} óñïåøíî ðàçìåùåíî");
        }

        return building;
    }

    private GameObject CreateBuildingObject(BuildingData data, Vector2 position)
    {

        GameObject obj = Instantiate(data.Prefab, position, Quaternion.identity);
        return obj;
    }

    private Building InitializeBuildingComponent(GameObject obj, BuildingData data)
    {
        Building building = obj.AddComponent<Building>();
        building.Initialize(data);

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(data.Width, data.Height);
        collider.offset = new Vector2(data.Width / 2f, data.Height / 2f);

        return building;
    }

    public bool CanPlaceBuilding(BuildingData data, Vector2 position)
    {
        Debug.Log($"WorldManager: Ïðîâåðêà âîçìîæíîñòè ðàçìåùåíèÿ {data.DisplayName} íà {position}");

        // Ïðîâåðêà êîëëèçèé
        Collider2D[] collisions = Physics2D.OverlapBoxAll(
            position,
            new Vector2(data.Width, data.Height),
            0,
            _buildingObstacleMask);

        if (collisions.Length > 0)
        {
            Debug.Log($"WorldManager: Íàéäåíû êîëëèçèè: {collisions.Length}");
            foreach (var col in collisions)
            {
                Debug.Log($"- {col.name}");
            }
            return false;
        }

        // Ïðîâåðêà ñïåöèàëüíûõ ïðàâèë
        if (data.PlacementRules != null)
        {
            foreach (PlacementRule rule in data.PlacementRules)
            {
                if (!rule.IsSatisfied(position))
                {
                    Debug.Log($"WorldManager: Ïðàâèëî {rule.name} íå âûïîëíåíî");
                    return false;
                }
            }
        }

        Debug.Log("WorldManager: Âñå ïðîâåðêè ïðîéäåíû");
        return true;
    }
}
