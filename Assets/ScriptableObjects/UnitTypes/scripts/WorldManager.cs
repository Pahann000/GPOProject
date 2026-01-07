using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance;

    private Texture2D _worldNoise;
    private Texture2D _goldNoise;
    private Texture2D _rootsNoise;
    private Texture2D _iceNoise;
    private GameObject[] _chunks;
    private List<Building> _activeBuildings = new List<Building>();

    [Header("Atlases")]
    public BlockAtlas blockAtlas;
    public UnitAtlas unitAtlas;

    [Header("World Generation Settings")]
    public float Seed;
    public int chunkSize = 16;
    public int WorldWidth = 10;
    public int WorldHigh = 100;
    public int HighAddition = 50;
    public float TerrainFreq = 0.04f;
    public float HighMultiplier = 25f;

    [Header("Cave Settings")]
    [Range(0.01f, 0.1f)] public float CaveFreq = 0.05f;
    [Range(0.1f, 0.9f)] public float CaveSize = 0.25f;

    [Header("Gold Settings")]
    [Range(0.01f, 0.1f)] public float GoldFrequency = 0.05f;
    [Range(0.1f, 0.9f)] public float GoldSize = 0.6f;

    [Header("Roots Settings")]
    [Range(0.01f, 0.1f)] public float RootsFrequency = 0.04f;
    [Range(0.1f, 0.9f)] public float RootsSize = 0.5f;
    [Range(0, 100)] public int MaxRootsHeight = 30;

    [Header("Ice Settings")]
    [Range(0.01f, 0.1f)] public float IceFrequency = 0.03f;
    [Range(0.1f, 0.9f)] public float IceSize = 0.7f;
    [Range(0, 100)] public int MinIceHeight = 70;

    [Header("Building Settings")]
    [SerializeField] private LayerMask _buildingObstacleMask;

    [Header("Настройки для системы строительства")]
    [SerializeField] private List<BlockType> _buildableBlockTypes = new List<BlockType>();
    [SerializeField] private List<BlockType> _resourceBlockTypes = new List<BlockType>();

    // Кэш блоков для быстрого доступа
    private Dictionary<Vector2Int, Block> _blockCache = new Dictionary<Vector2Int, Block>();

    void Awake()
    {
        Instance = this;
        InitializeBuildableBlocks();
    }

    void Start()
    {
        Seed = Random.Range(-100000, 100000);
        GenerateWorld();
    }

    // Инициализируем списки пригодных и ресурсных блоков
    private void InitializeBuildableBlocks()
    {
        // Автоматически заполняем список пригодных блоков
        if (_buildableBlockTypes.Count == 0 && blockAtlas != null)
        {
            // Предполагаем, что Rock - пригодный блок
            if (blockAtlas.Rock != null)
            {
                _buildableBlockTypes.Add(blockAtlas.Rock);
                Debug.Log($"Добавлен пригодный блок: {blockAtlas.Rock.Name}");
            }
        }

        // Автоматически заполняем список ресурсных блоков
        if (_resourceBlockTypes.Count == 0 && blockAtlas != null)
        {
            if (blockAtlas.Minerals != null)
            {
                _resourceBlockTypes.Add(blockAtlas.Minerals);
                Debug.Log($"Добавлен ресурсный блок: {blockAtlas.Minerals.Name}");
            }
            if (blockAtlas.Root != null)
            {
                _resourceBlockTypes.Add(blockAtlas.Root);
                Debug.Log($"Добавлен ресурсный блок: {blockAtlas.Root.Name}");
            }
            if (blockAtlas.Ice != null)
            {
                _resourceBlockTypes.Add(blockAtlas.Ice);
                Debug.Log($"Добавлен ресурсный блок: {blockAtlas.Ice.Name}");
            }
        }
    }

    private void GenerateWorld()
    {
        Debug.Log("Начинаю генерацию мира...");

        // Создаем чанки
        CreateChunks();

        // Генерируем шумовые текстуры
        _worldNoise = GenerateNoiseTexture(CaveFreq, CaveSize);
        _goldNoise = GenerateNoiseTexture(GoldFrequency, GoldSize);
        _rootsNoise = GenerateNoiseTexture(RootsFrequency, RootsSize);
        _iceNoise = GenerateNoiseTexture(IceFrequency, IceSize);

        // Генерируем террейн
        GenerateTerrain();

        Debug.Log($"Кэш блоков содержит: {_blockCache.Count} блоков");
    }

    public void CreateChunks()
    {
        _chunks = new GameObject[WorldWidth];
        for (int i = 0; i < WorldWidth; i++)
        {
            GameObject newChunk = new GameObject($"Chunk_{i}");
            newChunk.transform.parent = transform;
            _chunks[i] = newChunk;
        }
    }

    private void GenerateTerrain()
    {
        int totalBlocks = 0;
        int goldBlocks = 0;
        int rootsBlocks = 0;
        int iceBlocks = 0;
        int rockBlocks = 0;

        // Очищаем кэш
        _blockCache.Clear();

        for (int x = 0; x < WorldWidth * chunkSize; x++)
        {
            // Высота поверхности в этой колонке
            float surfaceHeight = Mathf.PerlinNoise((x + Seed) * TerrainFreq, Seed * TerrainFreq) * HighMultiplier + HighAddition;

            for (int y = 0; y < surfaceHeight && y < WorldHigh; y++)
            {
                // Пропускаем пустые места (пещеры)
                if (_worldNoise.GetPixel(x, y).r <= CaveSize)
                    continue;

                BlockType blockType = DetermineBlockType(x, y, surfaceHeight);

                // Статистика
                if (blockType == blockAtlas.Minerals) goldBlocks++;
                else if (blockType == blockAtlas.Root) rootsBlocks++;
                else if (blockType == blockAtlas.Ice) iceBlocks++;
                else if (blockType == blockAtlas.Rock) rockBlocks++;

                totalBlocks++;

                // Размещаем блок и добавляем в кэш
                Block placedBlock = PlaceBlockWithCache(blockType, x, y);
                if (placedBlock != null)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    _blockCache[pos] = placedBlock;
                }
            }
        }

        // Отладочная информация
        Debug.Log($"=== СТАТИСТИКА ГЕНЕРАЦИИ МИРА ===");
        Debug.Log($"Всего блоков: {totalBlocks}");
        Debug.Log($"Камень: {rockBlocks}");
        Debug.Log($"Золото: {goldBlocks}");
        Debug.Log($"Корни: {rootsBlocks}");
        Debug.Log($"Лед: {iceBlocks}");
        Debug.Log($"Блоков в кэше: {_blockCache.Count}");

        if (totalBlocks == 0)
            Debug.LogError("Мир не сгенерирован! Проверьте параметры генерации.");
    }

    private BlockType DetermineBlockType(int x, int y, float surfaceHeight)
    {
        // 1. Проверяем лед (только в верхней части мира)
        if (y >= MinIceHeight && y >= surfaceHeight * 0.7f)
        {
            if (_iceNoise.GetPixel(x, y).r > IceSize)
            {
                return blockAtlas.Ice;
            }
        }

        // 2. Проверяем золото (может быть везде)
        if (_goldNoise.GetPixel(x, y).r > GoldSize)
        {
            return blockAtlas.Minerals;
        }

        // 3. Проверяем корни (только в нижней части мира и ближе к поверхности)
        if (y <= MaxRootsHeight && y >= surfaceHeight * 0.3f)
        {
            if (_rootsNoise.GetPixel(x, y).r > RootsSize)
            {
                return blockAtlas.Root;
            }
        }

        // 4. По умолчанию - камень
        return blockAtlas.Rock;
    }

    private Texture2D GenerateNoiseTexture(float frequency, float limit)
    {
        Texture2D noise = new Texture2D(WorldWidth * chunkSize, WorldHigh);

        for (int x = 0; x < noise.width; x++)
        {
            for (int y = 0; y < noise.height; y++)
            {
                // Используем разные смещения для разных текстур
                float xCoord = (x + Seed) * frequency;
                float yCoord = (y + Seed) * frequency;

                float value = Mathf.PerlinNoise(xCoord, yCoord);

                // Normalize значение для лучшей видимости
                noise.SetPixel(x, y, value > limit ? Color.white : Color.black);
            }
        }

        noise.Apply();
        return noise;
    }

    // Обновленный метод размещения блоков с возвратом ссылки на Block
    private Block PlaceBlockWithCache(BlockType blockType, int x, int y)
    {
        if (blockType == null)
        {
            Debug.LogWarning($"Попытка разместить null блок в ({x}, {y})");
            return null;
        }

        GameObject newTile = new GameObject($"Block_{x}_{y}_{blockType.Name}");

        // Определяем чанк
        int chunkIndex = Mathf.FloorToInt(x / (float)chunkSize);
        chunkIndex = Mathf.Clamp(chunkIndex, 0, _chunks.Length - 1);
        newTile.transform.parent = _chunks[chunkIndex].transform;

        // Добавляем компоненты
        Block block = newTile.AddComponent<Block>();
        block.BlockType = blockType;
        block.CurrentHealth = blockType.Hardness;

        SpriteRenderer renderer = newTile.AddComponent<SpriteRenderer>();
        renderer.sprite = blockType.Sprite;
        renderer.sortingOrder = y; // Более высокие блоки рисуются поверх

        BoxCollider2D collider = newTile.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;

        // Назначаем слой в зависимости от типа блока
        if (_resourceBlockTypes.Contains(blockType))
        {
            newTile.layer = LayerMask.NameToLayer("Obstacle"); // Ресурсы - препятствия
            collider.isTrigger = false;
        }
        else if (_buildableBlockTypes.Contains(blockType))
        {
            newTile.layer = LayerMask.NameToLayer("Buildable"); // Пригодные блоки
            collider.isTrigger = false;
        }

        newTile.transform.position = new Vector3(x, y, 0);

        return block;
    }

    // Старый метод для обратной совместимости
    public void PlaceBlock(BlockType blockType, int x, int y)
    {
        PlaceBlockWithCache(blockType, x, y);
    }

    /// <summary>
    /// Размещает юнита в мире.
    /// </summary>
    /// <param name="unitType">Тип юнита.</param>
    /// <param name="x">x мировая координата.</param>
    /// <param name="y">y мировая координата</param>
    /// <returns>Созданный юнит.</returns>
    public Unit PlaceUnit(UnitType unitType, int x, int y)
    {
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
    /// Функция, вызываемая при уничтожении блока.
    /// </summary>
    /// <param name="block"></param>
    public void DestroyBlock(Block block)
    {
        // Удаляем из кэша
        Vector2Int pos = new Vector2Int(
            Mathf.RoundToInt(block.transform.position.x),
            Mathf.RoundToInt(block.transform.position.y)
        );

        if (_blockCache.ContainsKey(pos))
        {
            _blockCache.Remove(pos);
        }

        // Добавляем ресурсы при разрушении блока
        if (block.BlockType == blockAtlas.Minerals)
        {
            ResourceManager.Instance.AddResource(ResourceType.Minerals, Random.Range(1, 3));
            Debug.Log($"Добыто золото! +{Random.Range(1, 3)} минералов");
        }
        else if (block.BlockType == blockAtlas.Root)
        {
            ResourceManager.Instance.AddResource(ResourceType.Root, Random.Range(1, 2));
            Debug.Log($"Добыты корни! +{Random.Range(1, 2)} еды");
        }
        else if (block.BlockType == blockAtlas.Ice)
        {
            ResourceManager.Instance.AddResource(ResourceType.Ice, Random.Range(2, 4));
            Debug.Log($"Добыт лед! +{Random.Range(2, 4)} воды");
        }

        Debug.Log($"Блок {block.BlockType.Name} уничтожен");
    }

    /// <summary> Размещает здание с проверкой условий </summary>
    public Building PlaceBuilding(BuildingData data, Vector2 position)
    {
        Debug.Log($"WorldManager: Попытка разместить {data.DisplayName} на {position}");

        if (!CanPlaceBuilding(data, position))
        {
            Debug.Log("WorldManager: Нельзя разместить здание (не пройдены проверки)");
            return null;
        }

        if (!ResourceManager.Instance.TrySpendResources(data.ConstructionCost))
        {
            Debug.Log("WorldManager: Не удалось списать ресурсы");
            return null;
        }

        GameObject buildingObj = CreateBuildingObject(data, position);
        Building building = InitializeBuildingComponent(buildingObj, data);

        if (building != null)
        {
            _activeBuildings.Add(building);
            Debug.Log($"WorldManager: Здание {data.DisplayName} успешно размещено");
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

        BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(data.Width, data.Height);
        collider.offset = new Vector2(data.Width / 2f, data.Height / 2f);

        return building;
    }

    public bool CanPlaceBuilding(BuildingData data, Vector2 position)
    {
        Debug.Log($"WorldManager: Проверка возможности размещения {data.DisplayName} на {position}");

        // Проверка коллизий
        Collider2D[] collisions = Physics2D.OverlapBoxAll(
            position,
            new Vector2(data.Width, data.Height),
            0,
            _buildingObstacleMask);

        if (collisions.Length > 0)
        {
            Debug.Log($"WorldManager: Найдены коллизии: {collisions.Length}");
            foreach (var col in collisions)
            {
                Debug.Log($"- {col.name}");
            }
            return false;
        }

        // Проверка специальных правил
        if (data.PlacementRules != null)
        {
            foreach (PlacementRule rule in data.PlacementRules)
            {
                if (!rule.IsSatisfied(position))
                {
                    Debug.Log($"WorldManager: Правило {rule.name} не выполнено");
                    return false;
                }
            }
        }

        Debug.Log("WorldManager: Все проверки пройдены");
        return true;
    }

    /// <summary>
    /// Возвращает тип блока в указанных координатах.
    /// </summary>
    public BlockType GetBlockTypeAt(int x, int y)
    {
        // Сначала проверяем кэш для быстрого доступа
        Vector2Int pos = new Vector2Int(x, y);
        if (_blockCache.ContainsKey(pos))
        {
            return _blockCache[pos].BlockType;
        }

        // Если нет в кэше, ищем через физику
        Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector2(x, y), 0.1f);

        foreach (var collider in colliders)
        {
            Block block = collider.GetComponent<Block>();
            if (block != null)
            {
                // Добавляем в кэш для будущих запросов
                _blockCache[pos] = block;
                return block.BlockType;
            }
        }

        return null;
    }

    /// <summary>
    /// Проверяет, можно ли строить на блоке в указанных координатах.
    /// </summary>
    public bool IsBuildableAt(int x, int y)
    {
        BlockType blockType = GetBlockTypeAt(x, y);
        if (blockType == null) return false;

        // Проверяем, находится ли тип блока в списке пригодных
        return _buildableBlockTypes.Contains(blockType);
    }

    /// <summary>
    /// Проверяет, является ли блок ресурсом.
    /// </summary>
    public bool IsResourceBlockAt(int x, int y)
    {
        BlockType blockType = GetBlockTypeAt(x, y);
        if (blockType == null) return false;

        // Проверяем, находится ли тип блока в списке ресурсных
        return _resourceBlockTypes.Contains(blockType);
    }

    /// <summary>
    /// Получает список всех блоков в радиусе.
    /// </summary>
    public List<Block> GetBlocksInRadius(Vector2 position, float radius)
    {
        List<Block> blocks = new List<Block>();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(position, radius);

        foreach (var collider in colliders)
        {
            Block block = collider.GetComponent<Block>();
            if (block != null)
            {
                blocks.Add(block);
            }
        }

        return blocks;
    }

    /// <summary>
    /// Утилита для отладки - показывает информацию о блоке под мышью.
    /// </summary>
    [ContextMenu("Показать информацию о блоке под мышью")]
    public void DebugBlockUnderMouse()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int x = Mathf.RoundToInt(mousePos.x);
        int y = Mathf.RoundToInt(mousePos.y);

        BlockType blockType = GetBlockTypeAt(x, y);

        if (blockType != null)
        {
            Debug.Log($"Блок на ({x}, {y}): {blockType.Name}");
            Debug.Log($"Можно строить: {IsBuildableAt(x, y)}");
            Debug.Log($"Это ресурс: {IsResourceBlockAt(x, y)}");
        }
        else
        {
            Debug.Log($"На ({x}, {y}) нет блока");
        }
    }

    /// <summary>
    /// Проверяет область на возможность строительства.
    /// </summary>
    public bool CheckAreaForBuilding(Vector2 position, Vector2 size, float requiredPercentage = 0.7f)
    {
        int buildableCount = 0;
        int totalCount = 0;

        int startX = Mathf.FloorToInt(position.x - size.x / 2);
        int endX = Mathf.CeilToInt(position.x + size.x / 2);
        int startY = Mathf.FloorToInt(position.y - size.y / 2);
        int endY = Mathf.CeilToInt(position.y + size.y / 2);

        for (int x = startX; x <= endX; x++)
        {
            for (int y = startY; y <= endY; y++)
            {
                totalCount++;
                if (IsBuildableAt(x, y))
                {
                    buildableCount++;
                }
            }
        }

        float percentage = totalCount > 0 ? (float)buildableCount / totalCount : 0f;
        Debug.Log($"Проверка области: {buildableCount}/{totalCount} блоков пригодны ({percentage:P0})");

        return percentage >= requiredPercentage;
    }

    /// <summary>
    /// Удаляет блок из кэша.
    /// </summary>
    public void RemoveBlockFromCache(Block block)
    {
        if (block == null) return;

        Vector2Int pos = new Vector2Int(
            Mathf.RoundToInt(block.transform.position.x),
            Mathf.RoundToInt(block.transform.position.y)
        );

        if (_blockCache.ContainsKey(pos))
        {
            _blockCache.Remove(pos);
            Debug.Log($"Блок удален из кэша: {pos}");
        }
    }
}