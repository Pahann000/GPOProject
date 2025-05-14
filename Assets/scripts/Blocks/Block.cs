using UnityEngine;

public class Block : MonoBehaviour
{
    public BlockType BlockType;
    public int CurrentHealth;

    void Start() => CurrentHealth = BlockType.Hardness;

    public void TakeDamage(int damage, Player Damager)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DropResources(Damager);
            DestroyBlock();
        }
    }

    private void DestroyBlock()
    {
        Destroy(gameObject);
    }

    private void DropResources(Player player)
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
}