using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType BlockType;
    public int CurrentHealth;

    void Start()
    {
        // Автоматически добавляем коллайдер, если его нет
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }

        CurrentHealth = BlockType.Hardness;
    }

    public void TakeDamage(int damage)
    {
        Debug.Log($"блок уничтожается...({CurrentHealth}/{BlockType.Hardness})");
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DestroyBlock();
        }
    }

    public void DestroyBlock()
    {
        WorldManager.Instance.DestroyBlock(this);
        Destroy(gameObject);
    }
}