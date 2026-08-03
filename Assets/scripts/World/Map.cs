using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Класс игрового мира. Хранит сетку блоков и переносит процедурную генерацию в чанки.
/// </summary>
public class Map : MonoBehaviour
{
    private WorldConfig _config;
    private float _seed;
    private EventBus _eventBus;

    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Dictionary<Vector2Int, Block> _tileData = new Dictionary<Vector2Int, Block>();

    // Глобальный массив сгенерированного рельефа
    private BlockType[,] _worldMap;

    public int ChunkSize => _config.ChunkSize;
    public int Width => _config.WorldWidth * _config.ChunkSize;
    public int Height => _config.WorldHeight * _config.ChunkSize;

    public void Initialize(WorldConfig config, float seed, EventBus eventBus)
    {
        _config = config;
        _eventBus = eventBus;
        _seed = seed;

        GenerateWorldMap();
        Debug.Log("[Map] Карта инициализирована и шумы сгенерированы");
    }

    private void GenerateWorldMap()
    {
        _worldMap = new BlockType[Width, Height];

        // Шаг 1: Базовый ландшафт (камень) и искаженные жилы ресурсов (Domain Warping)
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                BlockType type = GetResourceTypeWithWarp(x, y);
                if (type == BlockType.Air)
                {
                    type = GetBaseRockType(x, y) ? BlockType.Rock : BlockType.Air;
                }
                _worldMap[x, y] = type;
            }
        }

        // Шаг 2: Извилистые пещеры и туннели (Drunkard's Walk)
        ApplyTunnels();

        // Шаг 3: Каньоны (Erosion)
        ApplyErosion();

        // Шаг 4: Убираем зависшие в воздухе ресурсы
        FixFloatingResources();
    }

    private BlockType GetResourceTypeWithWarp (int x, int y)
    {
        if (IsResourceActive(x, y, _config.MineralWarpSettings, BlockType.Minerals)) return BlockType.Minerals;
        if (IsResourceActive(x, y, _config.RootWarpSettings, BlockType.Root)) return BlockType.Root;
        if (IsResourceActive(x, y, _config.IceWarpSettings, BlockType.Ice)) return BlockType.Ice;
        return BlockType.Air;
    }

    private bool IsResourceActive(int x, int y, DomainWarpingSettings settings, BlockType type)
    {
        // Искажённые координаты
        float warpX = x + settings.warpAmplitude * Mathf.PerlinNoise(
            (x + _seed + settings.seedOffset) * settings.warpFrequency,
            (y + _seed + settings.seedOffset + 1000) * settings.warpFrequency);
        float warpY = y + settings.warpAmplitude * Mathf.PerlinNoise(
            (x + _seed + settings.seedOffset + 2000) * settings.warpFrequency,
            (y + _seed + settings.seedOffset + 3000) * settings.warpFrequency);

        float value = Mathf.PerlinNoise(
            (warpX + _seed + settings.seedOffset) * settings.frequency,
            (warpY + _seed + settings.seedOffset) * settings.frequency);

        return value > settings.threshold;
    }

    private bool GetBaseRockType(int x, int y)
    {
        float value = Mathf.PerlinNoise((x + _seed) * _config.CaveFreq, (y + _seed) * _config.CaveFreq);
        return value > _config.CaveSize;
    }

    private void ApplyTunnels()
    {
        System.Random rand = new System.Random((int)_seed);
        var settings = _config.TunnelSettings;

        for (int i = 0; i < settings.numberOfStartingPoints; i++)
        {
            int startX = rand.Next(0, Width);
            int startY = rand.Next(settings.minY, Mathf.Min(settings.maxY, Height - 1));
            DigTunnel(startX, startY, settings.maxStepsPerTunnel, 0, rand, settings);
        }
    }

    private void DigTunnel(int x, int y, int stepsLeft, int depth, System.Random rand, DrunkardWalkSettings settings)
    {
        if (stepsLeft <= 0 || depth > settings.maxBranchDepth) return;
        if (x < 0 || x >= Width || y < settings.minY || y >= Mathf.Min(settings.maxY, Height)) return;

        // Рисуем круг
        for (int dx = -settings.tunnelRadius; dx <= settings.tunnelRadius; dx++)
        {
            for (int dy = -settings.tunnelRadius; dy <= settings.tunnelRadius; dy++)
            {
                if (dx * dx + dy * dy <= settings.tunnelRadius * settings.tunnelRadius)
                {
                    int nx = x + dx;
                    int ny = y + dy;
                    if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                    {
                        _worldMap[nx, ny] = BlockType.Air;
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
        if (rand.NextDouble() < settings.branchProbability)
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
            DigTunnel(bx, by, stepsLeft / 2, depth + 1, rand, settings);
        }

        DigTunnel(nx2, ny2, stepsLeft - 1, depth, rand, settings);
    }

    private void ApplyErosion()
    {
        System.Random rand = new System.Random((int)_seed + 1000);
        var settings = _config.CanyonErosionSettings;

        for (int iter = 0; iter < settings.iterations; iter++)
        {
            for (int c = 0; c < settings.numberOfCanyons; c++)
            {
                int startX = rand.Next(0, Width);
                int surfaceY = -1;

                for (int y = Height - 1; y >= 0; y--)
                {
                    if (_worldMap[startX, y] != BlockType.Air)
                    {
                        surfaceY = y;
                        break;
                    }
                }
                if (surfaceY == -1) continue;

                int currentX = startX;
                int currentY = surfaceY;
                int depth = 0;

                while (depth < settings.maxDepth && currentY > 0)
                {
                    for (int dx = -settings.canyonRadius; dx <= settings.canyonRadius; dx++)
                    {
                        for (int dy = -settings.canyonRadius; dy <= settings.canyonRadius; dy++)
                        {
                            if (dx * dx + dy * dy <= settings.canyonRadius * settings.canyonRadius)
                            {
                                int nx = currentX + dx;
                                int ny = currentY + dy;
                                if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                                    _worldMap[nx, ny] = BlockType.Air;
                            }
                        }
                    }

                    if (rand.NextDouble() < settings.turnProbability)
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

    private void FixFloatingResources()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                BlockType type = _worldMap[x, y];
                if (type == BlockType.Minerals || type == BlockType.Root || type == BlockType.Ice)
                {
                    bool hasSupport = false;
                    int ny = y - 1;
                    if (ny >= 0 && _worldMap[x, ny] != BlockType.Air)
                    {
                        hasSupport = true;
                    }

                    if (!hasSupport)
                    {
                        _worldMap[x, y] = BlockType.Rock;
                    }
                }
            }
        }
    }

    private MapChunk CreateChunk(Vector2Int chunkPos)
    {
        GameObject chunkObj = new GameObject($"Chunk_{chunkPos.x}_{chunkPos.y}");
        chunkObj.transform.SetParent(transform);
        chunkObj.transform.position = new Vector3(
            chunkPos.x * ChunkSize,
            chunkPos.y * ChunkSize,
            0
        );

        MapChunk chunk = chunkObj.AddComponent<MapChunk>();
        chunk.Init(ChunkSize, _config.BlockAtlas, _config.AtlasMaterial);
        return chunk;
    }

    /// <summary>
    /// Ставит новый блок на указанное место.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="type"> Тип блока. </param>
    public void PlaceBlock(int x, int y, BlockType type)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return;

        Vector2Int pos = new Vector2Int(x, y);
        _tileData[pos] = new Block(new BlockData(type), x, y);
        _worldMap[x, y] = type;

        Vector2Int chunkPos = GetChunkPosition(x, y);
        if (_chunks.ContainsKey(chunkPos))
        {
            GenerateChunk(chunkPos.x, chunkPos.y);
        }
    }

    /// <summary>
    /// Получает объект блока, привязанного к миру
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns> <see cref="Block"/> привязанный к карте. </returns>
    public Block GetBlockObj(int x, int y)
    {
        Block block = GetBlockInfo(x, y);

        Vector2Int pos = new Vector2Int(x, y);
        _tileData[pos] = block;

        return block;
    }

    /// <summary>
    /// получает объект блока не привязанного к миру.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns> <see cref="Block"/> не привязанный к карте. </returns>
    public Block GetBlockInfo(int x, int y)
    {
        // Проверка границ
        if (x > Width || x < 0 || y > Height || y < 0)
        {
            return new Block(new BlockData(BlockType.Air), x, y);
        }

        Vector2Int pos = new Vector2Int(x, y);
        // Если блок уже был создан, возвращаем его
        if (_tileData.TryGetValue(pos, out Block data))
        {
            return data;
        }

        // Иначе берем тип из предсгенерированной карты мира
        BlockType type = _worldMap[x, y];
        return new Block(new BlockData(type), x, y);
    }

    /// <summary>
    /// Получает позицию чанка по мировым координатам. 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns> <see cref="Vector2Int"/> - позиция чанка </returns>
    public Vector2Int GetChunkPosition(int x, int y)
    {
        return new Vector2Int(
            Mathf.FloorToInt(x / (float)ChunkSize),
            Mathf.FloorToInt(y / (float)ChunkSize)
        );
    }

    /// <summary>
    /// Генерирует объект чанка.
    /// </summary>
    /// <param name="x"> Позиция чанка по X. </param>
    /// <param name="y"> Позиция чанка по Y. </param>
    public void GenerateChunk(int x, int y)
    {
        Vector2Int chunkPos = new Vector2Int(x, y);
        if (!_chunks.TryGetValue(chunkPos, out MapChunk chunk))
        {
            chunk = CreateChunk(chunkPos);
            _chunks.Add(chunkPos, chunk);

            _eventBus?.Raise(new ChunkGeneratedEvent(x, y, ChunkSize));
        }

        chunk.needsUpdate = true;
    }

    /// <summary>
    /// Уничтожает чанк по его координатам.
    /// </summary>
    /// <param name="x"> Позиция чанка по X. </param>
    /// <param name="y"> Позиция чанка по Y. </param>
    public void DestroyChunk(int x, int y)
    {
        Vector2Int chunkPos = new Vector2Int(x, y);
        if (_chunks.TryGetValue(chunkPos, out MapChunk chunk))
        {
            _chunks.Remove(chunkPos);
            Destroy(chunk.gameObject);
        }
    }

    ///// <summary>
    ///// Очищает всю карту. 
    ///// P.S. добавил на будущее.
    ///// </summary>
    //private void CleanUp()
    //{
    //    foreach (var chunk in _chunks.Values)
    //    {
    //        if (chunk != null) Destroy(chunk.gameObject);
    //    }
    //    _chunks.Clear();
    //    _tileData.Clear();
    //    _noises.Clear();
    //}
}