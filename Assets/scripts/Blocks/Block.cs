using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ����� �����.
/// </summary>
public class Block : MonoBehaviour
{
    /// <summary>
    /// ��� �����.
    /// </summary>
    public BlockType BlockType;
    /// <summary>
    /// ������� �������� �����. 
    /// </summary>
    public int CurrentHealth { get; private set; }

    ///<inheritdoc/>
    void Start() => CurrentHealth = BlockType.Hardness;

    /// <summary>
    /// ������� ��������� ����� ������.
    /// </summary>
    /// <param name="damage">���������� �����</param>
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
    /// �����, ���������� ��� ����������� �����.
    /// </summary>
    private void DestroyBlock()
    {
        Destroy(gameObject);
    }
}