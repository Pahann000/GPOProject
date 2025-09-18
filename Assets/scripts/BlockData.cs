[System.Serializable]
public class BlockData
{
    public BlockType type;
    public int health;

    public BlockData(BlockType type)
    {
        this.type = type;
        this.health = GetDefaultHealth(type);
    }

    private int GetDefaultHealth(BlockType type)
    {
        return type switch
        {
            BlockType.Stone => 50,
            BlockType.Gold => 100,
            _ => 0
        };
    }
}