[System.Serializable]
public class Tile
{
    private readonly Map _map;
    public int CurrentHealh { get; private set; }
    public TileData tileData;
    public readonly int x;
    public readonly int y;

    public Tile(TileData tileData, Map map, int x, int y)
    {
        _map = map;
        this.x = x;
        this.y = y;
        this.tileData = tileData;
        CurrentHealh = 10;
    }

    private void Destroy()
    {
        _map.SetTile(x, y, TileType.Air);
    }

    public void TakeDamage(int amount)
    {
        CurrentHealh -= amount;
        if (CurrentHealh <= 0)
        {
            Destroy();
        }
    }
}
