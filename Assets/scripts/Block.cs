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

    /// <summary>
    /// Можно ли строить на этом блоке.
    /// </summary>
    [Tooltip("Можно ли строить на этом блоке")]
    public bool IsBuildable = true;

    /// <summary>
    /// Является ли этот блок ресурсом (нельзя строить).
    /// </summary>
    [Tooltip("Является ли этот блок ресурсом (нельзя строить)")]
    public bool IsResource = false;

    ///<inheritdoc/>
    void Start()
    {
        CurrentHealth = BlockType.Hardness;

        // Автоматически определяем свойства блока по его типу
        if (BlockType != null)
        {
            string typeName = BlockType.Name.ToLower();

            // Определяем, можно ли строить на этом блоке
            IsBuildable = typeName.Contains("rock") ||
                         typeName.Contains("dirt") ||
                         typeName.Contains("sand") ||
                         typeName.Contains("grass") ||
                         typeName.Contains("земля") ||
                         typeName.Contains("камень");

            // Определяем, является ли блок ресурсом
            IsResource = typeName.Contains("mineral") ||
                        typeName.Contains("ice") ||
                        typeName.Contains("root") ||
                        typeName.Contains("gold") ||
                        typeName.Contains("золото") ||
                        typeName.Contains("лед") ||
                        typeName.Contains("корень");

            // Назначаем слой в зависимости от типа
            if (IsResource)
            {
                gameObject.layer = LayerMask.NameToLayer("Obstacle");
                Debug.Log($"Блок {BlockType.Name} назначен на слой Obstacle");
            }
            else if (IsBuildable)
            {
                gameObject.layer = LayerMask.NameToLayer("Buildable");
                Debug.Log($"Блок {BlockType.Name} назначен на слой Buildable");
            }
        }
    }

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
        // Уведомляем WorldManager об уничтожении блока
        if (WorldManager.Instance != null)
        {
            WorldManager.Instance.DestroyBlock(this);
        }
        else
        {
            Debug.LogWarning("WorldManager.Instance is null!");
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Вызывается при уничтожении объекта
    /// </summary>
    void OnDestroy()
    {
        // Дополнительная очистка, если нужно
        if (WorldManager.Instance != null)
        {
            // Удаляем из кэша WorldManager, если он использует кэш
            WorldManager.Instance.RemoveBlockFromCache(this);
        }
    }

    /// <summary>
    /// Возвращает информацию о блоке для отладки.
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Block: {BlockType?.Name}, Position: {transform.position}, " +
               $"Layer: {LayerMask.LayerToName(gameObject.layer)}, " +
               $"IsBuildable: {IsBuildable}, IsResource: {IsResource}";
    }
}