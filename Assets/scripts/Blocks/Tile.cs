[System.Serializable]
public class Tile
{
    private readonly Map _map;
    public int CurrentHealth { get; private set; }
    public TileData tileData;
    public readonly int x;
    public readonly int y;

    public Tile(TileData tileData, Map map, int x, int y)
    {
        _map = map;
        this.x = x;
        this.y = y;
        this.tileData = tileData;
        CurrentHealth = 10;
    }

    private void Destroy()
    {
        _map.SetTile(x, y, TileType.Air);
    }

    private void DropResources(Player player)
    {
        if (!player.Resources.ContainsKey(tileData.type.ToString()))
        {
            player.Resources.Add(tileData.type.ToString(), 1);
        }
        else
        {
            player.Resources[tileData.type.ToString()] += 1;
        }
    }

    public void TakeDamage(int amount, Player Damager)
    {
        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            DropResources(Damager);
            Destroy();
        }
    }
}
