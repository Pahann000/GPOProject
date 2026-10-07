/// <summary>
/// Интерфейс для любого объекта, способного получать урон.
/// </summary>
public interface IDamagable
{
    /// <summary>
    /// Наносит урон объекту. Множители урона считает атакующий:
    /// получателю передаётся уже итоговое значение.
    /// </summary>
    /// <param name="amount"> Количество урона. </param>
    /// <param name="damager"> Игрок, нанёсший урон. </param>
    void TakeDamage(int amount, Player damager);

    /// <summary>
    /// Текущее здоровье объекта.
    /// </summary>
    int CurrentHealth { get; }

    /// <summary>
    /// Положение объекта по X.
    /// </summary>
    int X { get; }

    /// <summary>
    /// Положение объекта по Y.
    /// </summary>
    int Y { get; }
}
