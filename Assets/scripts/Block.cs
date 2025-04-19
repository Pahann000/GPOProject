using UnityEngine;

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
    public int CurrentHealth;

    ///<inheritdoc/>
    void Start() => CurrentHealth = BlockType.Hardness;

    /// <summary>
    /// Функция получения урона блоком.
    /// </summary>
    /// <param name="damage">Количество урона</param>
    public void TakeDamage(int damage)
    {
        Debug.Log($"блок уничтожается...({CurrentHealth}/{BlockType.Hardness})");
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DestroyBlock();
        }
    }

    /// <summary>
    /// Метод, вызываемый при уничтожении блока.
    /// </summary>
    private void DestroyBlock()
    {
        WorldManager.Instance.DestroyBlock(this);
        Destroy(gameObject);
    }
}