using UnityEngine;

/// <summary>
/// Фасад для управления генерацией мира. 
/// Скрывает в себе работу с объектами Map, MapChunk и ChunkManager.
/// Предоставляет удобный API для получения информации о блоках на карте.
/// </summary>
public class WorldSystem : IGameSystem
{
    public string SystemName => "World System";

    private GameKernel _kernel;

    // Ссылки на старые MonoBehaviour генераторы
    private WorldManager _worldManager;

    // TODO: В будущем избавиться от синглтонов Map и ChunkManager
    // и хранить ссылки на них прямо здесь.

    /// <summary>
    /// Kill Switch: Включает или выключает физическое обновление чанков на сцене.
    /// Полезно для отладки производительности.
    /// </summary>
    public bool IsActive
    {
        get => _worldManager != null && _worldManager.enabled;
        set
        {
            if (_worldManager != null)
            {
                _worldManager.enabled = value;
                if (ChunkManager.Instance != null)
                {
                    ChunkManager.Instance.enabled = value;
                }
            }
        }
    }

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        // Находим генератор мира на сцене
        _worldManager = Object.FindFirstObjectByType<WorldManager>();

        if (_worldManager == null)
        {
            Debug.LogError($"[{SystemName}] CRITICAL: WorldManager не найден на сцене! Карта не сгенерируется.");
            return;
        }

        Debug.Log($"[{SystemName}] Успешно привязана к WorldManager.");
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
        if (Map.Instance == null) return BlockType.Air;

        Block block = Map.Instance.GetBlockInfo(x, y);
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
        if (Map.Instance == null) return false;

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