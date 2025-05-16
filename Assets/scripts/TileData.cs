[System.Serializable]
public class TileData
{
    public TileType type;
    public int health;

    public TileData(TileType type)
    {
        this.type = type;
        this.health = GetDefaultHealth(type);
    }

    private int GetDefaultHealth(TileType type)
    {
        return type switch
        {
            TileType.Stone => 50,
            TileType.Gold => 100,
            _ => 0
        };
    }
}