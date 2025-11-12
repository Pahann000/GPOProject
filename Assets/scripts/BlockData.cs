/// <summary>
/// Хранит информацию об отдельном блоке.
/// </summary>
[System.Serializable]
public class BlockData
{
    public BlockType type;
    public int health;

    /// <summary>
    /// Конмтруктор класса <see cref="BlockData"/>
    /// </summary>
    /// <param name="type"> тип блока. </param>
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