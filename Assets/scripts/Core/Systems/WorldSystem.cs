using Mirror;
using UnityEngine;

/// <summary>
/// Фасад для управления генерацией мира. 
/// Скрывает в себе работу с объектами Map, MapChunk и ChunkManager.
/// Предоставляет удобный API для получения информации о блоках на карте.
/// </summary>
public class WorldSystem : NetworkBehaviour, IGameSystem
{
    public string SystemName => "World System";

    private GameKernel _kernel;

    private bool _initialized;

    [SyncVar(hook = nameof(OnSeedChanged))]
    private float _seed; 

    // TODO: В будущем избавиться от синглтонов Map и ChunkManager
    // и хранить ссылки на них прямо здесь.

    /// <summary>
    /// Kill Switch: Включает или выключает физическое обновление чанков на сцене.
    /// Полезно для отладки производительности.
    /// </summary>
    public bool IsActive { get; set; } = true;
    public Map WorldMap { get; private set; }
    public ChunkManager WorldChunks { get; private set; }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Сервер генерирует случайный сид
        _seed = UnityEngine.Random.Range(0, 100000);
        Debug.Log($"server seed: {_seed}");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        Destroy(WorldMap.gameObject);
        Destroy(WorldChunks.gameObject);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"client seed: {_seed}");
    }

    private void OnSeedChanged(float oldSeed, float newSeed)
    {
        _seed = newSeed;
        GenerateWorld(_seed);
    }

    void GenerateWorld(float seed)
    {
        GenerateMapObject();
        WorldChunks = GenerateChunkManager();
    }

    private void GenerateMapObject()
    {
        GameObject MapGameObject = new GameObject("Map");
        Map map = MapGameObject.AddComponent<Map>();
        WorldConfig config = Resources.Load<WorldConfig>("MainWorldConfig");

        map.Initialize(config, _seed);
        WorldMap = map;
    }

    private ChunkManager GenerateChunkManager()
    {
        GameObject chunkManagerGameObject = new GameObject("ChunkManager");
        ChunkManager chunkManager = chunkManagerGameObject.AddComponent<ChunkManager>();
        chunkManager.Initialize(WorldMap);
        return chunkManager;
    }

    private void Start()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.RegisterSystem(this);
            if (!_initialized)
            {
                Initialize(GameKernel.Instance);
                _initialized = true;
            }
        }
        else
        {
            Debug.LogError($"[{SystemName}] GameKernel не найден!");
        }
    }

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        WorldConfig config = Resources.Load<WorldConfig>("MainWorldConfig");
        if (config == null)
        {
            Debug.LogError($"[{SystemName}] CRITICAL: Не найден MainWorldConfig в Resources!");
            return;
        }

        Debug.Log($"[{SystemName}] Мир создан.");
    }

    public void Tick(float deltaTime)
    {
        // TODO: Логика чанков пока крутится в их собственных методах Update/FixedUpdate, что не есть хорошо.
        // В будущем можно перенести вызов ChunkManager.UpdateRequiredChunks() сюда.
    }

    public void FixedTick(float deltaTime) { }

    public void Shutdown() { }

    /// <summary>
    /// Получает тип блока (земля, воздух, камень) по мировым координатам.
    /// </summary>
    public BlockType GetBlockTypeAt(int x, int y)
    {
        if (WorldMap == null) return BlockType.Air;

        Block block = WorldMap.GetBlockInfo(x, y);
        return block != null ? block.tileData.type : BlockType.Air;
    }

    /// <summary>
    /// Проверяет, можно ли разместить здание заданного размера в указанной позиции.
    /// </summary>
    /// <param name="position">Центр здания.</param>
    /// <param name="size">Ширина и высота здания.</param>
    /// <param name="minPercentage">Какой процент блоков фундамента должен быть твердым (0.5 = 50%).</param>
    /// <returns>True, если место пригодно для стройки.</returns>
    public bool IsSurfaceBuildable(Vector2 position, Vector2 size, float minPercentage = 0.5f)
    {
        if (WorldMap == null) return false;

        // Проверяем, что ВНУТРИ самого здания (в его центре) находится воздух.
        // Это предотвращает постройку зданий прямо внутри скалы.
        // TODO: Доработать, чтобы проверял все блоки занимаемые зданием.
        BlockType blockInside = GetBlockTypeAt(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        if (blockInside != BlockType.Air)
        {
            return false;
        }

        // Проверяем ФУНДАМЕНТ (блоки ровно под зданием).
        // TODO: Доработать так как может быть на половину на блоке, то есть пловоина здания свисает.
        float bottomY = position.y - (size.y / 2f) - 0.5f;
        float leftX = position.x - (size.x / 2f);
        float rightX = position.x + (size.x / 2f);

        int validGroundBlocks = 0;
        int totalGroundBlocks = 0;

        // Проходим с шагом 1 по всей ширине фундамента
        for (float x = leftX + 0.5f; x < rightX; x += 1f)
        {
            totalGroundBlocks++;
            BlockType blockUnder = GetBlockTypeAt(Mathf.FloorToInt(x), Mathf.FloorToInt(bottomY));

            if (IsBuildableBlockType(blockUnder))
            {
                validGroundBlocks++;
            }
        }

        float supportPercentage = totalGroundBlocks > 0 ? (float)validGroundBlocks / totalGroundBlocks : 0f;
        return supportPercentage >= minPercentage;
    }

    /// <summary>
    /// Определяет, является ли конкретный тип блока пригодным для установки на него зданий.
    /// </summary>
    private bool IsBuildableBlockType(BlockType blockType)
    {
        if (blockType == BlockType.Air) return false; // В воздухе строить нельзя

        string blockName = blockType.ToString().ToLower();

        // На ресурсах (золото, руда, лед) строить нельзя — их нужно сначала добыть
        if (blockName.Contains("gold") || blockName.Contains("mineral") ||
            blockName.Contains("ice") || blockName.Contains("root"))
        {
            return false;
        }

        // Обычные блоки (камень, земля) считаются твердой почвой
        return true;
    }
}