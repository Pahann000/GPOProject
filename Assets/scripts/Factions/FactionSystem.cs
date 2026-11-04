using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Серверное распределение игроков по фракциям и создание их баз.
/// </summary>
/// <remarks>
/// Правила:
/// <list type="bullet">
///   <item>первый подключившийся игрок попадает к людям, второй — к марсианам,
///   дальше фракции чередуются по числу игроков в каждой;</item>
///   <item>люди появляются на поверхности, марсиане — в пещере под землёй;</item>
///   <item>для каждой фракции расчищается площадка (и лестница на поверхность
///   у марсиан), чтобы на ней можно было строить и ходить.</item>
/// </list>
///
/// Позиции баз — это SyncVar, поэтому клиенты расчищают ту же территорию
/// локально: правки блоков детерминированы и не требуют по RPC на каждый блок.
/// Игрок, подключившийся позже, получает базы уже в стартовом пакете спавна.
/// </remarks>
public class FactionSystem : NetworkBehaviour, IGameSystem
{
    public string SystemName => "Faction System";
    public bool IsActive { get; set; } = true;

    [Header("Префабы")]
    [Tooltip("Маркер базы фракции. Спавнится на сервере при появлении базы.")]
    [SerializeField] private GameObject factionBasePrefab;

    private GameKernel _kernel;
    private WorldSystem _worldSystem;

    /// <summary>Базы сервера: колонка центра и уровень пола.</summary>
    private readonly Dictionary<FactionType, Vector2Int> _bases = new Dictionary<FactionType, Vector2Int>();

    /// <summary>Игроки, для которых ещё не выбрана точка появления.</summary>
    private readonly List<Player> _pendingPlayers = new List<Player>();

    /// <summary>Игроки, зачтённые во фракции (для подсчёта баланса).</summary>
    private readonly HashSet<Player> _knownPlayers = new HashSet<Player>();

    /// <summary>Базы, уже расчищенные этим клиентом.</summary>
    private readonly HashSet<FactionType> _appliedOnClient = new HashSet<FactionType>();

    private int _humansCount;
    private int _martiansCount;

    [SyncVar(hook = nameof(OnHumanBaseChanged))]
    private Vector2Int _humanBase;

    [SyncVar(hook = nameof(OnMartianBaseChanged))]
    private Vector2Int _martianBase;

    private bool _humanBaseKnown;
    private bool _martianBaseKnown;

    /// <summary>Атлас фракций из Resources.</summary>
    public FactionAtlas Atlas => FactionAtlas.Cached;

    public int HumansCount => _humansCount;
    public int MartiansCount => _martiansCount;

    private void Start()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.RegisterSystem(this);
        }
        else
        {
            Debug.LogError($"[{SystemName}] GameKernel не найден!");
        }
    }

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        ResolveWorldSystem();

        if (Atlas == null)
        {
            Debug.LogError($"[{SystemName}] Не найден FactionAtlas в Resources: фракции не будут работать.");
        }

        Debug.Log($"[{SystemName}] Инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        if (!ResolveWorldSystem()) return;

        if (isServer)
        {
            ResolvePendingPlayers();
            return;
        }

        ApplyBasesOnClient();
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        _bases.Clear();
        _pendingPlayers.Clear();
        _knownPlayers.Clear();
        _appliedOnClient.Clear();
    }

    /// <summary>
    /// Пытается получить <see cref="WorldSystem"/>. Мир создаётся префабом
    /// NetworkSystems, поэтому на первых кадрах его может ещё не быть.
    /// </summary>
    private bool ResolveWorldSystem()
    {
        if (_worldSystem != null && _worldSystem.WorldMap != null) return true;

        GameKernel kernel = _kernel != null ? _kernel : GameKernel.Instance;
        if (kernel == null) return false;

        kernel.TryGetSystem(out _worldSystem);

        return _worldSystem != null && _worldSystem.WorldMap != null;
    }

    /// <summary>
    /// Назначает игроку фракцию и ставит его в очередь на выбор точки появления.
    /// Вызывается сервером из <see cref="Player.OnStartServer"/>.
    /// </summary>
    public void AssignFaction(Player player)
    {
        if (!isServer || player == null) return;
        if (player.Faction != FactionType.None) return;

        FactionType faction = FactionSpawnMath.SelectFaction(_humansCount, _martiansCount);

        if (faction == FactionType.Humans) _humansCount++;
        else _martiansCount++;

        player.SetFactionServer(faction);

        _knownPlayers.Add(player);
        if (!_pendingPlayers.Contains(player)) _pendingPlayers.Add(player);

        Debug.Log($"[{SystemName}] Игрок {player.name} зачислен во фракцию {faction} "
            + $"(людей: {_humansCount}, марсиан: {_martiansCount}).");
    }

    /// <summary>
    /// Убирает игрока из учёта фракций: следующий подключившийся займёт его место.
    /// </summary>
    public void UnregisterPlayer(Player player)
    {
        if (player == null) return;
        if (!_knownPlayers.Remove(player)) return;

        if (player.Faction == FactionType.Humans)
        {
            _humansCount = Mathf.Max(0, _humansCount - 1);
        }
        else if (player.Faction == FactionType.Martians)
        {
            _martiansCount = Mathf.Max(0, _martiansCount - 1);
        }

        _pendingPlayers.Remove(player);
    }

    /// <summary>
    /// Точка появления фракции (центр базы), если база уже создана.
    /// </summary>
    public bool TryGetSpawnPoint(FactionType faction, out Vector2 spawnPoint)
    {
        spawnPoint = Vector2.zero;

        if (!_bases.TryGetValue(faction, out Vector2Int info)) return false;

        spawnPoint = FactionSpawnMath.GetSpawnPosition(info.x, info.y);
        return true;
    }

    /// <summary>
    /// Точка появления юнитов: рядом с центром базы, внутри расчищенной зоны.
    /// </summary>
    public bool TryGetUnitSpawnPoint(FactionType faction, out Vector2 spawnPoint)
    {
        if (!TryGetSpawnPoint(faction, out spawnPoint)) return false;

        FactionDefinition definition = Atlas != null ? Atlas.Get(faction) : null;
        int offset = definition != null ? Mathf.Max(1, definition.ClearHalfWidth - 1) : 2;

        spawnPoint += new Vector2(offset, 0f);
        return true;
    }

    /// <summary>
    /// True, если база фракции уже создана сервером.
    /// </summary>
    public bool IsBaseResolved(FactionType faction)
    {
        return _bases.ContainsKey(faction);
    }

    /// <summary>Ставит игроков, ждущих появления, на их базы.</summary>
    private void ResolvePendingPlayers()
    {
        for (int i = _pendingPlayers.Count - 1; i >= 0; i--)
        {
            Player player = _pendingPlayers[i];

            if (player == null || player.Faction == FactionType.None)
            {
                _pendingPlayers.RemoveAt(i);
                continue;
            }

            // База нужна раньше игрока: именно она даёт точку появления.
            if (!ResolveBase(player.Faction)) continue;

            if (!TryGetSpawnPoint(player.Faction, out Vector2 spawnPoint)) continue;

            player.SetFactionSpawnServer(new Vector3(spawnPoint.x, spawnPoint.y, 0f));
            _pendingPlayers.RemoveAt(i);
        }
    }

    /// <summary>
    /// Создаёт базу фракции: расчищает территорию и ставит маркер базы.
    /// </summary>
    private bool ResolveBase(FactionType faction)
    {
        if (faction == FactionType.None) return false;
        if (_bases.ContainsKey(faction)) return true;

        FactionDefinition definition = Atlas != null ? Atlas.Get(faction) : null;
        if (definition == null)
        {
            Debug.LogError($"[{SystemName}] Для фракции {faction} нет описания в FactionAtlas.");
            return false;
        }

        Map map = _worldSystem.WorldMap;
        if (map == null) return false;

        int centerX = FactionSpawnMath.GetBaseColumn(definition, map.Width);

        if (!map.TryGetSurfaceWorldY(centerX, out float surfaceY))
        {
            Debug.LogWarning($"[{SystemName}] В колонке {centerX} нет твёрдых блоков: базу {faction} поставить некуда.");
            return false;
        }

        int floorY;
        if (definition.SpawnsOnSurface)
        {
            // Наземная база: пол чуть ниже верхней границы карты, чтобы над ней
            // оставалось место для зданий и юнитов.
            floorY = FactionSpawnMath.ClampSurfaceFloorY(
                map.Height,
                Mathf.RoundToInt(surfaceY),
                definition.ClearHeight + 1);
        }
        else
        {
            floorY = FactionSpawnMath.ComputeFloorY(
                Mathf.RoundToInt(surfaceY),
                false,
                definition.UndergroundDepth);
        }

        CarveTerritory(_worldSystem, definition, centerX, floorY);
        SpawnFactionBase(faction, centerX, floorY);

        _bases[faction] = new Vector2Int(centerX, floorY);

        // SyncVar: клиенты (в том числе подключившиеся позже) расчистят то же место.
        if (faction == FactionType.Humans)
        {
            _humanBase = new Vector2Int(centerX, floorY);
        }
        else
        {
            _martianBase = new Vector2Int(centerX, floorY);
        }

        Debug.Log($"[{SystemName}] База {faction} создана: колонка {centerX}, пол {floorY} "
            + $"(поверхность {surfaceY}, {(definition.SpawnsOnSurface ? "на поверхности" : "под землёй")}).");

        return true;
    }

    private void SpawnFactionBase(FactionType faction, int centerX, int floorY)
    {
        if (factionBasePrefab == null)
        {
            Debug.LogWarning($"[{SystemName}] Не назначен factionBasePrefab: маркер базы не создан.");
            return;
        }

        Vector2 spawnPoint = FactionSpawnMath.GetSpawnPosition(centerX, floorY);
        GameObject instance = Instantiate(factionBasePrefab, new Vector3(spawnPoint.x, spawnPoint.y, 0f), Quaternion.identity);

        FactionBase baseMarker = instance.GetComponent<FactionBase>();
        if (baseMarker != null)
        {
            // Фракция задаётся до Spawn, чтобы прилетела в стартовом пакете.
            baseMarker.SetFactionServer(faction);
        }
        else
        {
            Debug.LogWarning($"[{SystemName}] На префабе базы нет компонента FactionBase.");
        }

        NetworkServer.Spawn(instance);
    }

    /// <summary>
    /// Расчищает площадку фракции и делает пол пригодным для строительства.
    /// Функция детерминирована: сервер и клиенты получают одинаковый результат,
    /// поэтому по сети передаются только координаты базы.
    /// </summary>
    public static void CarveTerritory(WorldSystem world, FactionDefinition definition, int centerX, int floorY)
    {
        if (world == null || definition == null) return;

        Map map = world.WorldMap;
        if (map == null) return;

        RectInt clear = FactionSpawnMath.GetClearRect(
            centerX,
            floorY,
            definition.ClearHalfWidth,
            definition.ClearHeight);

        int minX = Mathf.Max(0, clear.xMin);
        int maxX = Mathf.Min(map.Width - 1, clear.xMax - 1);

        // Наземная база вырубается до самого верха карты: иначе над ней осталась бы
        // порода и «поверхности» не было бы видно. Подземная — ровно на высоту зала.
        int airTopY = definition.SpawnsOnSurface
            ? map.Height - 1
            : Mathf.Min(map.Height - 1, clear.yMax - 1);

        for (int x = minX; x <= maxX; x++)
        {
            // Свободное место под постройки и проход юнитов.
            for (int y = clear.yMin; y <= airTopY; y++)
            {
                if (y < 0 || y >= map.Height) continue;
                world.ApplyBlockLocal(x, y, BlockType.Air);
            }

            // Несущий пол: гарантирует опору зданиям и юнитам.
            int floorBlockY = clear.yMin - 1;
            if (floorBlockY >= 0 && floorBlockY < map.Height)
            {
                world.ApplyBlockLocal(x, floorBlockY, definition.FloorBlock);
            }

            // Над пещерой марсиан нужен свод, иначе она «висит» в породе.
            int ceilingY = clear.yMax;
            if (!definition.SpawnsOnSurface && ceilingY >= 0 && ceilingY < map.Height)
            {
                world.ApplyBlockLocal(x, ceilingY, definition.CeilingBlock);
            }
        }

        if (!definition.SpawnsOnSurface)
        {
            CarveExitStairs(world, map, centerX + Mathf.Max(1, definition.ClearHalfWidth), clear.yMax);
        }
    }

    /// <summary>
    /// Прокладывает из пещеры марсиан наклонный проход на поверхность
    /// (ступенька в один блок — юнит перепрыгивает её без специальных лифтов).
    /// </summary>
    private static void CarveExitStairs(WorldSystem world, Map map, int startX, int startY)
    {
        const int maxSteps = 64;

        for (int step = 0; step < maxSteps; step++)
        {
            int x = startX + step;
            int y = startY + step;

            if (x < 0 || x >= map.Width || y < 0 || y >= map.Height) return;

            // Проход высотой 2 блока: юнит (коллайдер 1x1) проходит свободно.
            world.ApplyBlockLocal(x, y, BlockType.Air);
            world.ApplyBlockLocal(x, y + 1, BlockType.Air);

            // Вышли на поверхность: дальше копать нечего. TryGetSurfaceWorldY
            // возвращает верхнюю грань породы, поэтому сравниваем с ней, а не с +1:
            // иначе лестница копала бы диагональ через всю карту.
            if (map.TryGetSurfaceWorldY(x, out float surfaceY) && y >= Mathf.FloorToInt(surfaceY) - 1)
            {
                return;
            }
        }
    }

    private void OnHumanBaseChanged(Vector2Int oldValue, Vector2Int newValue)
    {
        _humanBaseKnown = newValue != Vector2Int.zero;
        TryApplyBaseOnClient(FactionType.Humans, newValue);
    }

    private void OnMartianBaseChanged(Vector2Int oldValue, Vector2Int newValue)
    {
        _martianBaseKnown = newValue != Vector2Int.zero;
        TryApplyBaseOnClient(FactionType.Martians, newValue);
    }

    /// <summary>
    /// Догоняет расчистку на клиенте: SyncVar может прийти раньше, чем мир
    /// построен (порядок применения спавн-пакета не гарантирован).
    /// </summary>
    private void ApplyBasesOnClient()
    {
        if (isServer) return;

        if (_humanBaseKnown) TryApplyBaseOnClient(FactionType.Humans, _humanBase);
        if (_martianBaseKnown) TryApplyBaseOnClient(FactionType.Martians, _martianBase);
    }

    private void TryApplyBaseOnClient(FactionType faction, Vector2Int info)
    {
        if (isServer || faction == FactionType.None) return;
        if (_appliedOnClient.Contains(faction)) return;
        if (info == Vector2Int.zero) return;
        if (!ResolveWorldSystem()) return;

        FactionDefinition definition = Atlas != null ? Atlas.Get(faction) : null;
        if (definition == null) return;

        CarveTerritory(_worldSystem, definition, info.x, info.y);
        _appliedOnClient.Add(faction);
    }
}
