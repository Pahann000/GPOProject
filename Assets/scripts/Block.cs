using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType BlockType;
    public int CurrentHealth;

    void Start() => CurrentHealth = BlockType.Hardness;

    public void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DestroyBlock();
        }
    }

    void DestroyBlock()
    {
        WorldManager.Instance.DestroyBlock(this);
        Destroy(gameObject);
    }
}