using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Класс блока.
/// </summary>
public class Block : MonoBehaviour
{
    /// <summary>
    /// Тип блока.
    /// </summary>
    public BlockType BlockType;
    /// <summary>
    /// Текущее здоровье блока. 
    /// </summary>
    public int CurrentHealth { get; private set; }

    ///<inheritdoc/>
    void Start() => CurrentHealth = BlockType.Hardness;

    /// <summary>
    /// Функция получения урона блоком.
    /// </summary>
    /// <param name="damage">Количество урона</param>
    public void TakeDamage(int damage, Player damager)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DropRecources(damager);
            DestroyBlock();
        }
    }

    private void DropRecources(Player player)
    {
        if (!player.Resources.ContainsKey(BlockType.Name))
        {
            player.Resources.Add(BlockType.Name, 1);
        }
        else
        {
            player.Resources[BlockType.Name] += 1;
        }
    }

    /// <summary>
    /// Метод, вызываемый при уничтожении блока.
    /// </summary>
    private void DestroyBlock()
    {
        Destroy(gameObject);
    }
}