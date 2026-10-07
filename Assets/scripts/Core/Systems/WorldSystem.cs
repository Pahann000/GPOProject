using Mirror;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Фасад для управления генерацией мира. 
/// Скрывает в себе работу с объектами Map, MapChunk и ChunkManager.
/// Предоставляет удобный API для получения информации о блоках на карте.
/// </summary>
public class WorldSystem : NetworkBehaviour, IGameSystem
{

    private GameKernel _kernel;

    /// <summary>Конфиг мира загружается один раз и переиспользуется.</summary>
    private WorldConfig _worldConfig;

    [SyncVar(hook = nameof(OnSeedChanged))]
    private float _seed; 

    /// <summary>
    /// Kill Switch: Включает или выключает физическое обновление чанков на сцене.
    /// Полезно для отладки производительности.
    /// </summary>
    /// <summary>Сид, для которого на этом пире уже построен мир (NaN — мир ещё не строили).</summary>
    private float _builtSeed = float.NaN;

    public bool IsActive { get; set; } = true;
    public Map WorldMap { get; private set; }
    public ChunkManager WorldChunks { get; private set; }

    public string SystemName => "World System";

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Присвоение SyncVar вызывает хук OnSeedChanged (в том числе на сервере),
        // а EnsureWorld делает повторную генерацию no-op'ом.
        _seed = UnityEngine.Random.Range(0, 100000);
        EnsureWorld(_seed);
        Debug.Log($"server seed: {_seed}");
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        // WorldSystem живёт на NetworkSystems.prefab, а не на игроке, поэтому
        // isLocalPlayer здесь всегда false и объекты мира никогда не удалялись.
        DestroyWorldObjects();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"client seed: {_seed}");
    }

    private void OnSeedChanged(float oldSeed, float newSeed)
    {
        _seed = newSeed;
        EnsureWorld(_seed);
    }

    /// <summary>
    /// Строит мир, только если для этого сида он ещё не построен на данном пире.
    /// Раньше сервер строил мир дважды (в OnStartServer и в хуке от присвоения сида),
    /// а в режиме Host клиентский хук пересоздавал уже готовый мир сервера,
    /// теряя поставленные блоки.
    /// </summary>
    private void EnsureWorld(float seed)
    {
        if (WorldMap != null && Mathf.Approximately(_builtSeed, seed)) return;

        GenerateWorld(seed);
        _builtSeed = seed;
    }

    void GenerateWorld(float seed)
    {
        // Старые объекты мира обязательно уничтожаем: иначе повторная генерация
        // (смена сида) оставляла висячие Map/ChunkManager, и разные системы
        // читали блоки из разных карт.
        DestroyWorldObjects();

        WorldMap = GenerateMapObject();
        if (WorldMap == null) return;

        WorldChunks = GenerateChunkManager();
    }

    /// <summary>
    /// Уничтожает объекты мира и обнуляет ссылки на них.
    /// </summary>
    private void DestroyWorldObjects()
    {
        if (WorldMap != null) Destroy(WorldMap.gameObject);
        if (WorldChunks != null) Destroy(WorldChunks.gameObject);

        WorldMap = null;
        WorldChunks = null;
    }

    private Map GenerateMapObject()
    {
        if (_worldConfig == null)
        {
            _worldConfig = Resources.Load<WorldConfig>("MainWorldConfig");
        }

        if (_worldConfig == null)
        {
            Debug.LogError($"[{SystemName}] CRITICAL: не найден MainWorldConfig в Resources!");
            return null;
        }

        GameObject MapGameObject = new GameObject("Map");
        Map map = MapGameObject.AddComponent<Map>();

        map.Initialize(_worldConfig, _seed);
        return map;
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
            // Регистрация сама вызывает Initialize: сразу, если ядро уже стартовало,
            // либо из GameKernel.Start(). Раньше Initialize вызывался ещё и здесь,
            // поэтому система инициализировалась дважды.
            GameKernel.Instance.RegisterSystem(this);
        }
        else
        {
            Debug.LogError($"[{SystemName}] GameKernel не найден!");
        }
    }

    public void RequestPlaceBlock(int x, int y, BlockType blockType)
    {
        if (isServer)
        {
            WorldMap.PlaceBlock(x, y, blockType);
            RpcPlaceBlock(x, y, blockType);
        }
    }

    [ClientRpc]
    private void RpcPlaceBlock(int x, int y, BlockType blockType)
    {
        if (WorldMap == null) { return; }
        WorldMap.PlaceBlock(x, y, blockType);
    }

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        if (_worldConfig == null)
        {
            _worldConfig = Resources.Load<WorldConfig>("MainWorldConfig");
        }

        if (_worldConfig == null)
        {
            Debug.LogError($"[{SystemName}] CRITICAL: Не найден MainWorldConfig в Resources!");
            return;
        }

        Debug.Log($"[{SystemName}] Инициализирована.");
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
        return block != null ? block.tileData.Type : BlockType.Air;
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
    /// Блоки, на которых строить нельзя: их нужно сначала добыть.
    /// Раньше тип определялся сравнением blockType.ToString().ToLower() с подстроками
    /// ("gold", "mineral", "ice", "root") — это аллоцировало две строки на каждый блок
    /// фундамента в цикле и молча ломалось при переименовании enum.
    /// </summary>
    private static readonly HashSet<BlockType> UnbuildableBlocks = new HashSet<BlockType>
    {
        BlockType.Air,
        BlockType.Gold,
        BlockType.Minerals,
        BlockType.Ice,
        BlockType.Root
    };

    /// <summary>
    /// Определяет, является ли конкретный тип блока пригодным для установки на него зданий.
    /// </summary>
    private bool IsBuildableBlockType(BlockType blockType)
    {
        return !UnbuildableBlocks.Contains(blockType);
    }

    //временная штука для тестов
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private UnitAtlas unitAtlas;
    [Tooltip("Смещение колонки появления юнита по X относительно игрока.")]
    [SerializeField] private int unitSpawnOffsetX = 2;
    [Tooltip("На сколько колонок в сторону можно отойти, если в нужной нет земли.")]
    [SerializeField] private int unitSpawnSearchRadius = 6;

    [Command(requiresAuthority = false)]
    public void CmdSpawnUnit(Player player)
    {
        if (!isServer) return;

        if (player == null)
        {
            Debug.LogWarning($"[{SystemName}] CmdSpawnUnit: игрок не передан.");
            return;
        }

        if (unitPrefab == null || unitAtlas == null)
        {
            Debug.LogError($"[{SystemName}] CmdSpawnUnit: не заданы unitPrefab или unitAtlas.");
            return;
        }

        if (WorldMap == null)
        {
            Debug.LogWarning($"[{SystemName}] CmdSpawnUnit: мир ещё не создан.");
            return;
        }

        // Юнит ставится НА поверхность. Раньше он появлялся в точке «игрок + (2,1)»,
        // то есть внутри породы: ходить там некуда, путь не строился, и юнит
        // просто стоял, молча теряя приказ.
        if (!TryFindSurfaceSpawn(player.transform.position.x + unitSpawnOffsetX, out Vector3 spawnPosition))
        {
            Debug.LogWarning($"[{SystemName}] CmdSpawnUnit: рядом с игроком нет поверхности для появления юнита.");
            return;
        }

        GameObject unitObj = Instantiate(unitPrefab, spawnPosition, transform.rotation);
        Unit unit = unitObj.GetComponent<Unit>();

        if (unit == null)
        {
            Debug.LogError($"[{SystemName}] CmdSpawnUnit: на префабе юнита нет компонента Unit.");
            Destroy(unitObj);
            return;
        }

        unit.Owner = player;
        unit.unitType = unitAtlas.Miner;

        NetworkServer.Spawn(unitObj);
    }

    /// <summary>
    /// Ищет точку на поверхности рядом с указанной колонкой: сначала саму колонку,
    /// затем соседние в пределах <see cref="unitSpawnSearchRadius"/>.
    /// </summary>
    private bool TryFindSurfaceSpawn(float preferredX, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        int startX = Mathf.RoundToInt(preferredX);
        if (TryBuildSpawnPosition(startX, out spawnPosition)) return true;

        for (int offset = 1; offset <= unitSpawnSearchRadius; offset++)
        {
            if (TryBuildSpawnPosition(startX - offset, out spawnPosition)) return true;
            if (TryBuildSpawnPosition(startX + offset, out spawnPosition)) return true;
        }

        return false;
    }

    private bool TryBuildSpawnPosition(int x, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        if (WorldMap == null || !WorldMap.TryGetSurfaceWorldY(x, out float surfaceY)) return false;

        // Коллайдер юнита высотой 1, значит его центр стоит на полблока выше поверхности.
        spawnPosition = new Vector3(x + 0.5f, surfaceY + 0.5f, 0f);
        return true;
    }
}
