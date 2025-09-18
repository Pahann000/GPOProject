[System.Serializable]
public class Block : IDamagable
{
    private readonly Map _map;
    private readonly int x;
    private readonly int y;

    public BlockData tileData;
    public int CurrentHealth { get; private set; }

    public int X => x;

    public int Y => y;

    public Block(BlockData tileData, Map map, int x, int y)
    {
        this._map = map;
        this.x = x;
        this.y = y;
        this.tileData = tileData;
        CurrentHealth = tileData.health;
    }

    private void Destroy()
    {
        _map.PlaceBlock(x, y, BlockType.Air);
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

    public void TakeDamage(int amount, Player Damager, UnitTypeName unitType)
    {
        if (unitType == UnitTypeName.Miner)
        {
            CurrentHealth -= amount * 2;
        }
        else
        {
            CurrentHealth -= amount / 2;
        }

        if (CurrentHealth <= 0)
        {
            DropResources(Damager);
            Destroy();
        }
    }
}
