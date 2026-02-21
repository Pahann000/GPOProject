using UnityEngine;

public class WorldSystem : IGameSystem
{
    public string SystemName => "World System";

    private GameKernel _kernel;
    private WorldManager _worldManager;

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

        _worldManager = Object.FindFirstObjectByType<WorldManager>();

        if (_worldManager == null )
        {
            Debug.LogError($"[{SystemName}] CRITICAL: WorldManager не найден на сцене! Карта не сгенерирована.");
            return;
        }

        Debug.Log($"[{SystemName}] Успешно привязана к WorldManager.");
    }

    public void Tick(float deltaTime)
    {
        // пока не трогать
    }

    public void FixedTick(float deltaTime) { }

    public void Shutdown() { }

    public BlockType GetBlockTypeAt(int x, int y)
    {
        if (Map.Instance == null) return BlockType.Air;

        Block block = Map.Instance.GetBlockInfo(x, y);
        return block != null ? block.tileData.type : BlockType.Air;
    }

    public bool IsSurfaceBuildable(Vector2 position, Vector2 size, float checkPrecision = 0.5f, float minPercentage = 0.7f)
    {
        if (Map.Instance == null) return false;

        float bottomY = position.y - (size.y / 2f) - 0.5f;

        float leftX = position.x - (size.x / 2f);
        float rightX = position.x + (size.x / 2f);

        int validGroundBlocks = 0;
        int totalGroundBlocks = 0;

        BlockType blockInside = GetBlockTypeAt(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        if (blockInside != BlockType.Air)
        {
            return false;
        }

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

    private bool IsBuildableBlockType(BlockType blockType)
    {
        if (blockType == BlockType.Air) return false;

        string blockName = blockType.ToString().ToLower();

        if (blockName.Contains("gold") || blockName.Contains("mineral") ||
            blockName.Contains("ice") || blockName.Contains("root"))
        {
            return false;
        }

        return true;
    }
}