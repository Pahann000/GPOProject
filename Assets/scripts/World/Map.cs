using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Класс игрового мира.
/// </summary>
public class Map : MonoBehaviour
{
    private WorldConfig _config;
    private float _seed;
    private Dictionary<Vector2Int, MapChunk> _chunks = new Dictionary<Vector2Int, MapChunk>();
    private Dictionary<Vector2Int, Block> _tileData = new Dictionary<Vector2Int, Block>();

    /// <summary>
    /// Слои шума: тип блока и параметры формулы Перлина.
    /// Раньше это был словарь Texture2D, то есть шум пекся в две текстуры
    /// 1600x1600 (~5 млн вызовов SetPixel на старте и ~20 МБ памяти),
    /// а тип каждого блока читался через Texture2D.GetPixel.
    /// </summary>
    private struct NoiseLayer
    {
        public float Frequency;
        public float Limit;
        public float SeedOffset;
        public BlockType Type;
    }

    private readonly List<NoiseLayer> _noises = new List<NoiseLayer>();

    public int ChunkSize => _config.ChunkSize;
    public int Width => _config.WorldWidth * _config.ChunkSize;
    public int Height => _config.WorldHeight * _config.ChunkSize;

    /// <summary>
    /// Шина событий карты. Через неё сообщается о появлении чанков и об изменении
    /// блоков (это нужно миникарте). Может быть не задана — тогда просто нет подписчиков.
    /// </summary>
    public EventBus EventBus { get; set; }

    /// <summary>Позиции уже загруженных чанков (например, чтобы дорисовать миникарту).</summary>
    public IEnumerable<Vector2Int> LoadedChunks => _chunks.Keys;

    public void Initialize(WorldConfig config, float seed)
    {
        _config = config;
        _seed = seed;

        BuildNoiseLayers();

        Debug.Log("[Map] Карта инициализирована");
    }

    /// <summary>
    /// Порядок слоёв важен: первый слой, у которого значение шума больше порога,
    /// определяет тип блока (раньше это зависело от порядка обхода словаря).
    /// </summary>
    private void BuildNoiseLayers()
    {
        _noises.Clear();

        _noises.Add(new NoiseLayer
        {
            Frequency = _config.GoldFrequency,
            Limit = _config.GoldSize,
            SeedOffset = 100f,
            Type = BlockType.Gold
        });

        _noises.Add(new NoiseLayer
        {
            Frequency = _config.CaveFreq,
            Limit = _config.CaveSize,
            SeedOffset = 0f,
            Type = BlockType.Stone
        });
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
        chunk.Init(this, ChunkSize, _config.BlockAtlas, _config.AtlasMaterial);
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
        Vector2Int pos = new Vector2Int(x, y);
        _tileData[pos] = new Block(new BlockData(type), x, y);

        Vector2Int chunkPos = GetChunkPosition(x, y);
        if (_chunks.ContainsKey(chunkPos))
        {
            GenerateChunk(chunkPos.x, chunkPos.y);
        }

        // Один пиксель миникарты: блок поставлен или сломан.
        EventBus?.Raise(new BlockChangedEvent(x, y, type));
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
        Vector2Int pos = new Vector2Int(x, y);

        // Если блок уже был создан, возвращаем его
        if (_tileData.TryGetValue(pos, out Block data))
        {
            return data;
        }

        return new Block(new BlockData(GetBlockType(x, y)), x, y);
    }

    /// <summary>
    /// Возвращает тип блока, не создавая объекты <see cref="Block"/> и <see cref="BlockData"/>.
    /// Нужен для горячих путей (поиск пути, построение меша), где GetBlockInfo
    /// давал по две аллокации на каждую проверку клетки.
    /// </summary>
    public BlockType GetBlockType(int x, int y)
    {
        Vector2Int pos = new Vector2Int(x, y);
        if (_tileData.TryGetValue(pos, out Block data))
        {
            return data.tileData.Type;
        }

        return GetGeneratedBlockType(x, y);
    }

    /// <summary>
    /// Тип блока по одной генерации, без учёта поставленных игроком блоков.
    /// Нужен, чтобы понять, какие клетки игрок действительно изменил (для сохранения).
    /// </summary>
    public BlockType GetGeneratedBlockType(int x, int y)
    {
        // Проверка границ: индексы валидны до Width-1 / Height-1,
        // иначе GetPixel уходил за пределы текстуры шума.
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return BlockType.Air;
        }

        // Формула та же, что раньше запекалась в текстуру: value > limit.
        for (int i = 0; i < _noises.Count; i++)
        {
            NoiseLayer layer = _noises[i];
            float value = Mathf.PerlinNoise(
                (x + _seed + layer.SeedOffset) * layer.Frequency,
                (y + _seed + layer.SeedOffset) * layer.Frequency);

            if (value > layer.Limit)
            {
                return layer.Type;
            }
        }

        return BlockType.Air;
    }

    /// <summary>
    /// Список блоков, которые отличаются от генерации. Сохранять всю карту не нужно:
    /// сид восстанавливает её детерминированно, а хранить надо только правки игрока.
    /// </summary>
    public List<BlockChangeData> GetChangedBlocks()
    {
        var changed = new List<BlockChangeData>();

        foreach (KeyValuePair<Vector2Int, Block> pair in _tileData)
        {
            Block block = pair.Value;
            if (block == null) continue;

            // В кэше лежат и просто запрошенные блоки: они совпадают с генерацией.
            if (block.tileData.Type == GetGeneratedBlockType(pair.Key.x, pair.Key.y)) continue;

            changed.Add(new BlockChangeData
            {
                x = pair.Key.x,
                y = pair.Key.y,
                type = block.tileData.Type
            });
        }

        return changed;
    }

    /// <summary>Применяет правки блоков из сохранения (ставит их поверх сгенерированной карты).</summary>
    public void ApplyBlockChanges(List<BlockChangeData> changes)
    {
        if (changes == null) return;

        for (int i = 0; i < changes.Count; i++)
        {
            BlockChangeData change = changes[i];
            if (change == null) continue;

            PlaceBlock(change.x, change.y, change.type);
        }
    }

    /// <summary>
    /// Возвращает карту к сгенерированному состоянию: убирает правки блоков.
    /// Нужно перед загрузкой сохранения, иначе поверх него останутся правки текущей сессии.
    /// </summary>
    public int ClearBlockChanges()
    {
        var toRevert = new List<Vector2Int>();

        foreach (KeyValuePair<Vector2Int, Block> pair in _tileData)
        {
            Block block = pair.Value;
            if (block == null)
            {
                toRevert.Add(pair.Key);
                continue;
            }

            if (block.tileData.Type != GetGeneratedBlockType(pair.Key.x, pair.Key.y))
            {
                toRevert.Add(pair.Key);
            }
        }

        for (int i = 0; i < toRevert.Count; i++)
        {
            Vector2Int pos = toRevert[i];
            _tileData.Remove(pos);

            Vector2Int chunkPos = GetChunkPosition(pos.x, pos.y);
            if (_chunks.ContainsKey(chunkPos))
            {
                GenerateChunk(chunkPos.x, chunkPos.y);
            }

            // Миникарта должна вернуть исходный цвет клетки.
            BlockType generated = GetGeneratedBlockType(pos.x, pos.y);
            EventBus?.Raise(new BlockChangedEvent(pos.x, pos.y, generated));
        }

        return toRevert.Count;
    }

    /// <summary>
    /// Мировая высота верхней грани самого верхнего твёрдого блока в колонке.
    /// Нужна, чтобы ставить юнитов и объекты на поверхность, а не внутрь породы.
    /// </summary>
    /// <param name="x">Координата колонки.</param>
    /// <param name="surfaceWorldY">Высота верхней грани блока (например, для блока y=4 это 5).</param>
    /// <returns>False, если в колонке нет твёрдых блоков.</returns>
    public bool TryGetSurfaceWorldY(int x, out float surfaceWorldY)
    {
        surfaceWorldY = 0f;
        if (x < 0 || x >= Width) return false;

        // Идём сверху вниз: выше поверхности блоков нет по определению.
        for (int y = Height - 1; y >= 0; y--)
        {
            if (GetBlockType(x, y) == BlockType.Air) continue;

            surfaceWorldY = y + 1;
            return true;
        }

        return false;
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

            // Чанк появился впервые: миникарта дорисовывает его клетки.
            // При повторной генерации (постановка блока) событие не нужно —
            // об изменении сообщает PlaceBlock.
            EventBus?.Raise(new ChunkGeneratedEvent(chunkPos.x, chunkPos.y, ChunkSize));
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

    /// <summary>
    /// Очищает всю карту: чанки и кэш блоков.
    /// </summary>
    private void CleanUp()
    {
        foreach (var chunk in _chunks.Values)
        {
            if (chunk != null) Destroy(chunk.gameObject);
        }
        _chunks.Clear();
        _tileData.Clear();
        _noises.Clear();
    }

    private void OnDestroy()
    {
        CleanUp();
    }
}