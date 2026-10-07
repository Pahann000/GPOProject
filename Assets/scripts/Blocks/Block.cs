using Mirror;
using UnityEngine;

/// <summary>
/// Класс, хранящий информацию о блоке.
/// </summary>
[System.Serializable]
public class Block : IDamagable
{
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
    /// <param name="x"> Положение блока по X </param>
    /// <param name="y"> Положение блока по Y </param>
    public Block(BlockData tileData, int x, int y)
    {
        this.x = x;
        this.y = y;
        this.tileData = tileData;
        CurrentHealth = tileData.Health;
    }

    private void Destroy()
    {
        var kernel = GameKernel.Instance;
        if (kernel != null && kernel.TryGetSystem(out WorldSystem worldSystem))
        {
            worldSystem.RequestPlaceBlock(x, y, BlockType.Air);
        }
    }

    private void DropResources(Player player)
    {
        if (player == null) return;

        var kernel = GameKernel.Instance;
        if (kernel != null && kernel.TryGetSystem(out ResourceSystem resourceSystem))
        {
            resourceSystem.AddResource(player, new ResourcePair(tileData.DropResourceType, 1));
        }
    }

    /// <summary>
    /// Наносит урон блоку.
    /// После достижения определённого количества здоровья ломается и передаёт ресурсы игроку в инвентарь.
    /// </summary>
    /// <param name="amount"> Количество урона (итоговое, множители считает атакующий) </param>
    /// <param name="Damager"> Игрок, нанёсший урон. </param>
    public void TakeDamage(int amount, Player Damager)
    {
        // Мир и экономика — серверные: на клиенте ResourceSystem отсутствует,
        // и обращение к нему раньше падало с NullReferenceException.
        if (!NetworkServer.active) return;
        if (amount <= 0) return;

        // Защита от повторного дропа: без неё второй удар по уже разрушенному
        // блоку снова выдавал ресурсы.
        if (CurrentHealth <= 0) return;

        CurrentHealth -= amount;

        if (CurrentHealth > 0) return;

        CurrentHealth = 0;
        DropResources(Damager);
        Destroy();
    }
}
