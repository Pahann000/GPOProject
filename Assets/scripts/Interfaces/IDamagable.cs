/// <summary>
/// Интерфейс для любого объекта, способного получать урон.
/// </summary>
public interface IDamagable
{
    /// <summary>
    /// Наносит урон объекту.
    /// </summary>
    /// <param name="amount"> Количество урона. </param>
    /// <param name="Damager"> Игрок, нанёсший урон. </param>
    /// <param name="unitType"> Тип юнита, нанёсшего урон. </param>
    public void TakeDamage(int amount, Player Damager, UnitTypeName unitType);

    /// <summary>
    /// Текущее здоровье объекта.
    /// </summary>
    public int CurrentHealth { get; }

    /// <summary>
    /// Положение объекта по X.
    /// </summary>
    public int X { get; }

    /// <summary>
    /// Положение объекта по X.
    /// </summary>
    public int Y { get; }
}
