using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [SerializeField] private BlockAtlas atlas;
    [SerializeField] private Material atlasMaterial;
    [SerializeField] private LayerMask _buildingObstacleMask;
    [SerializeField] private List<BlockType> _buildableBlockTypes = new List<BlockType>();
    [SerializeField] private List<BlockType> _resourceBlockTypes = new List<BlockType>();

    // Поля для генерации (оставлены без изменений)
    public float CaveSize = 0.25f;
    public float CaveFreq = 0.05f;
    public float TerrainFreq = 0.04f;
    public float HighMultiplier = 25f;
    public float Seed;
    public int chunkSize = 16;
    public int WorldWidth = 100;
    public int WorldHeight = 100;
    public int HighAddition = 50;

    // Настройки для ресурсов (больше не создаём текстуры, только параметры)
    [Header("Mineral Settings")]
    [Range(0.01f, 0.1f)] public float MineralFrequency = 0.05f;
    [Range(0.1f, 0.9f)] public float MinaralSize = 0.6f;

    [Header("Roots Settings")]
    [Range(0.01f, 0.1f)] public float RootsFrequency = 0.04f;
    [Range(0.1f, 0.9f)] public float RootsSize = 0.5f;
    [Range(0, 100)] public int MaxRootsHeight = 30;

    [Header("Ice Settings")]
    [Range(0.01f, 0.1f)] public float IceFrequency = 0.03f;
    [Range(0.1f, 0.9f)] public float IceSize = 0.7f;
    [Range(0, 100)] public int MinIceHeight = 70;

    // ==================== НОВЫЕ НАСТРОЙКИ ====================
    [Header("Drunkard's Walk (Tunnels)")]
    public DrunkardWalkSettings tunnelSettings;

    [Header("Domain Warping (Resources)")]
    public DomainWarpingSettings mineralWarpSettings;
    public DomainWarpingSettings rootWarpSettings;
    public DomainWarpingSettings iceWarpSettings;

    [Header("Erosion (Canyons)")]
    public ErosionSettings erosionSettings;

    [System.Serializable]
    public class DrunkardWalkSettings
    {
        public int numberOfStartingPoints = 10;
        public int maxStepsPerTunnel = 100;
        public int tunnelRadius = 2;
        [Range(0f, 1f)] public float branchProbability = 0.1f;
        public int maxBranchDepth = 3;
        public int minY = 10;
        public int maxY = 90;
    }

    [System.Serializable]
    public class DomainWarpingSettings
    {
        public float frequency = 0.05f;
        [Range(0f, 1f)] public float threshold = 0.6f;
        public float warpAmplitude = 5f;
        public float warpFrequency = 0.02f;
        public int seedOffset = 0;
    }

    [System.Serializable]
    public class ErosionSettings
    {
        public int numberOfCanyons = 15;
        public int maxDepth = 30;
        public int canyonRadius = 2;
        [Range(0f, 1f)] public float turnProbability = 0.3f;
        public int iterations = 10;
    }
    // =========================================================

    private List<Building> _activeBuildings = new List<Building>();
    private Dictionary<Texture2D, BlockType> Noises = new(); // больше не используется, но оставим для совместимости
    private Map _map; // ссылка на созданную карту
    private BlockType[,] worldMap; // глобальный массив

    void Awake()
    {
        InitializeBuildableBlocks();
    }

    private void InitializeBuildableBlocks()
    {
        if (_buildableBlockTypes.Count == 0)
        {
            _buildableBlockTypes.Add(BlockType.Rock);
            Debug.Log($"Добавлен пригодный блок: {BlockType.Rock}");
        }

        if (_resourceBlockTypes.Count == 0)
        {
            _resourceBlockTypes.Add(BlockType.Minerals);
            _resourceBlockTypes.Add(BlockType.Root);
            _resourceBlockTypes.Add(BlockType.Ice);
            Debug.Log("Добавлены ресурсные блоки");
        }
    }

    void Start()
    {
        Seed = Random.Range(-100000f, 100000f);
        GenerateMapObject();          // создаём объект Map
        GenerateWorldMap();           // генерируем мир в массив
        TransferToMap();              // передаём массив в Map
        GenerateChunkManager();       // создаём менеджер чанков (он подгрузит их вокруг наблюдателя)
    }

    private Map GenerateMapObject()
    {
        GameObject mapGameObject = new GameObject("Map");
        _map = mapGameObject.AddComponent<Map>();
        _map.atlas = atlas;
        _map.atlasMaterial = atlasMaterial;
        _map.Noises = Noises;         // можно оставить пустым
        _map.width = WorldWidth * chunkSize;
        _map.height = WorldHeight * chunkSize;
        return _map;
    }

    private ChunkManager GenerateChunkManager()
    {
        GameObject chunkManagerGameObject = new GameObject("ChunkManager");
        ChunkManager chunkManager = chunkManagerGameObject.AddComponent<ChunkManager>();
        return chunkManager;
    }

    // ==================== ГЕНЕРАЦИЯ МИРА ====================
    private void GenerateWorldMap()
    {
        int w = WorldWidth * chunkSize;
        int h = WorldHeight * chunkSize;
        worldMap = new BlockType[w, h];

        // Шаг 1: базовый ландшафт (камень) и ресурсы с warp
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                BlockType type = GetResourceTypeWithWarp(x, y);
                if (type == BlockType.Air)
                {
                    type = GetBaseRockType(x, y) ? BlockType.Rock : BlockType.Air;
                }
                worldMap[x, y] = type;
            }
        }

        // Шаг 2: туннели
        ApplyTunnels();

        // Шаг 3: каньоны
        ApplyErosion();
    }

    private BlockType GetResourceTypeWithWarp(int x, int y)
    {
        if (IsResourceActive(x, y, mineralWarpSettings, BlockType.Minerals)) return BlockType.Minerals;
        if (IsResourceActive(x, y, rootWarpSettings, BlockType.Root)) return BlockType.Root;
        if (IsResourceActive(x, y, iceWarpSettings, BlockType.Ice)) return BlockType.Ice;
        return BlockType.Air;
    }

    private bool IsResourceActive(int x, int y, DomainWarpingSettings settings, BlockType type)
    {
        // Искажённые координаты
        float warpX = x + settings.warpAmplitude * Mathf.PerlinNoise(
            (x + Seed + settings.seedOffset) * settings.warpFrequency,
            (y + Seed + settings.seedOffset + 1000) * settings.warpFrequency);
        float warpY = y + settings.warpAmplitude * Mathf.PerlinNoise(
            (x + Seed + settings.seedOffset + 2000) * settings.warpFrequency,
            (y + Seed + settings.seedOffset + 3000) * settings.warpFrequency);

        float value = Mathf.PerlinNoise(
            (warpX + Seed + settings.seedOffset) * settings.frequency,
            (warpY + Seed + settings.seedOffset) * settings.frequency);
        return value > settings.threshold;
    }

    private bool GetBaseRockType(int x, int y)
    {
        float value = Mathf.PerlinNoise((x + Seed) * CaveFreq, (y + Seed) * CaveFreq);
        return value > CaveSize;
    }

    private void ApplyTunnels()
    {
        int w = worldMap.GetLength(0);
        int h = worldMap.GetLength(1);
        System.Random rand = new System.Random((int)Seed);

        for (int i = 0; i < tunnelSettings.numberOfStartingPoints; i++)
        {
            int startX = rand.Next(0, w);
            int startY = rand.Next(tunnelSettings.minY, tunnelSettings.maxY + 1);
            DigTunnel(startX, startY, tunnelSettings.maxStepsPerTunnel, 0, rand);
        }
    }

    private void DigTunnel(int x, int y, int stepsLeft, int depth, System.Random rand)
    {
        if (stepsLeft <= 0 || depth > tunnelSettings.maxBranchDepth) return;
        if (x < 0 || x >= worldMap.GetLength(0) || y < tunnelSettings.minY || y > tunnelSettings.maxY) return;

        // Рисуем круг
        for (int dx = -tunnelSettings.tunnelRadius; dx <= tunnelSettings.tunnelRadius; dx++)
        {
            for (int dy = -tunnelSettings.tunnelRadius; dy <= tunnelSettings.tunnelRadius; dy++)
            {
                if (dx * dx + dy * dy <= tunnelSettings.tunnelRadius * tunnelSettings.tunnelRadius)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < worldMap.GetLength(0) && ny >= 0 && ny < worldMap.GetLength(1))
                    {
                        worldMap[nx, ny] = BlockType.Air;
                    }
                }
            }
        }

        int dir = rand.Next(4);
        int nx2 = x, ny2 = y;
        switch (dir)
        {
            case 0: ny2++; break;
            case 1: ny2--; break;
            case 2: nx2--; break;
            case 3: nx2++; break;
        }

        // Ветвление
        if (rand.NextDouble() < tunnelSettings.branchProbability)
        {
            int branchDir = (dir + 1 + rand.Next(2) * 2) % 4;
            int bx = x, by = y;
            switch (branchDir)
            {
                case 0: by++; break;
                case 1: by--; break;
                case 2: bx--; break;
                case 3: bx++; break;
            }
            DigTunnel(bx, by, stepsLeft / 2, depth + 1, rand);
        }

        DigTunnel(nx2, ny2, stepsLeft - 1, depth, rand);
    }

    private void ApplyErosion()
    {
        int w = worldMap.GetLength(0);
        int h = worldMap.GetLength(1);
        System.Random rand = new System.Random((int)Seed + 1000);

        for (int iter = 0; iter < erosionSettings.iterations; iter++)
        {
            for (int c = 0; c < erosionSettings.numberOfCanyons; c++)
            {
                int startX = rand.Next(0, w);
                // находим поверхность (самый верхний не-Air блок)
                int surfaceY = -1;
                for (int y = h - 1; y >= 0; y--)
                {
                    if (worldMap[startX, y] != BlockType.Air)
                    {
                        surfaceY = y;
                        break;
                    }
                }
                if (surfaceY == -1) continue;

                int currentX = startX;
                int currentY = surfaceY;
                int depth = 0;

                while (depth < erosionSettings.maxDepth && currentY > 0)
                {
                    // вырезаем круг
                    for (int dx = -erosionSettings.canyonRadius; dx <= erosionSettings.canyonRadius; dx++)
                    {
                        for (int dy = -erosionSettings.canyonRadius; dy <= erosionSettings.canyonRadius; dy++)
                        {
                            if (dx * dx + dy * dy <= erosionSettings.canyonRadius * erosionSettings.canyonRadius)
                            {
                                int nx = currentX + dx;
                                int ny = currentY + dy;
                                if (nx >= 0 && nx < w && ny >= 0 && ny < h)
                                    worldMap[nx, ny] = BlockType.Air;
                            }
                        }
                    }

                    // движение
                    if (rand.NextDouble() < erosionSettings.turnProbability)
                    {
                        currentX += rand.Next(2) == 0 ? -1 : 1;
                    }
                    else
                    {
                        currentY--;
                    }
                    depth++;
                }
            }
        }
    }

    private void TransferToMap()
    {
        if (_map == null)
            _map = FindObjectOfType<Map>();
        if (_map != null)
            _map.SetWorldData(worldMap);
        else
            Debug.LogError("Map not found!");
    }
    // =========================================================

    // Методы размещения зданий (без изменений)
    public Building PlaceBuilding(BaseBuildingData data, Vector2 position)
    {
        Debug.Log($"WorldManager: Попытка разместить {data.DisplayName} на {position}");
        if (!CanPlaceBuilding(data, position))
        {
            Debug.Log("WorldManager: Нельзя разместить здание");
            return null;
        }
        if (!ResourceManager.Instance.TrySpendResources(data.ConstructionCost))
        {
            Debug.Log("WorldManager: Не удалось списать ресурсы");
            return null;
        }

        GameObject buildingObj = Instantiate(data.Prefab, position, Quaternion.identity);
        Building building = buildingObj.GetComponent<Building>();
        building.Initialize(data);

        BoxCollider2D collider = buildingObj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(data.Width, data.Height);
        collider.offset = new Vector2(data.Width / 2f, data.Height / 2f);

        _activeBuildings.Add(building);
        Debug.Log($"WorldManager: Здание {data.DisplayName} успешно размещено");
        return building;
    }

    public bool CanPlaceBuilding(BaseBuildingData data, Vector2 position)
    {
        Collider2D[] collisions = Physics2D.OverlapBoxAll(
            position,
            new Vector2(data.Width, data.Height),
            0,
            _buildingObstacleMask);
        if (collisions.Length > 0) return false;

        if (data.PlacementRules != null)
        {
            foreach (PlacementRule rule in data.PlacementRules)
            {
                if (!rule.IsSatisfied(position))
                    return false;
            }
        }
        return true;
    }
}