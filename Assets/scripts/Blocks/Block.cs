/// <summary>
/// Класс, хранящий информацию о облоке.
/// </summary>
[System.Serializable]
public class Block : IDamagable
{
    private readonly Map _map;
    private readonly int x;
    private readonly int y;

    /// <summary>
    /// информация о типе блока
    /// </summary>
    public BlockData tileData { get; private set; }

    /// <summary>
    /// Текущее здоровье блока.
    /// </summary>
    public int CurrentHealth { get; private set; }

    /// <summary>
    /// Положение блока по X
    /// </summary>
    public int X => x;

    /// <summary>
    /// Положение блока по Y
    /// </summary>
    public int Y => y;

    /// <summary>
    /// Конструктор класса <see cref="Block"/>.
    /// </summary>
    /// <param name="tileData"> Информация о блоке. </param>
    /// <param name="map"> Родительский объект карты, на которой находится блок. </param>
    /// <param name="x"> Положение блока по X </param>
    /// <param name="y"> Положение блока по Y </param>
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

    /// <summary>
    /// Наносит урон блоку.
    /// После достижения определённого количества здоровья ломается и передаёт ресурсы игроку в инвентарь.
    /// </summary>
    /// <param name="amount"> Количество урона </param>
    /// <param name="Damager"> Игрок, нанёсший урон. </param>
    /// <param name="unitType"> Тип юнита, нанёсшего урон. </param>
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
