/// <summary>
/// Хранит информацию об отдельном блоке.
/// </summary>
[System.Serializable]
public class BlockData
{
    public BlockType Type;
    public int Health;
    public ResourceType DropResourceType;

    /// <summary>
    /// Конмтруктор класса <see cref="BlockData"/>
    /// </summary>
    /// <param name="type"> тип блока. </param>
    public BlockData(BlockType type)
    {
        this.Type = type;
        this.Health = GetDefaultHealth(type);
        this.DropResourceType = GetResourceType(type);
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

    private ResourceType GetResourceType(BlockType type)
    {
        return type switch
        {
            BlockType.Stone => ResourceType.Rock,
            BlockType.Gold => ResourceType.Minerals,
            _ => 0
        };

    }
}