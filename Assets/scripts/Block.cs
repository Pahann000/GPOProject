using UnityEngine;

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
    public int CurrentHealth;

    ///<inheritdoc/>
    void Start() => CurrentHealth = BlockType.Hardness;

    /// <summary>
    /// ������� ��������� ����� ������.
    /// </summary>
    /// <param name="damage">���������� �����</param>
    public void TakeDamage(int damage)
    {
        Debug.Log($"���� ������������...({CurrentHealth}/{BlockType.Hardness})");
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DestroyBlock();
        }
    }

    /// <summary>
    /// �����, ���������� ��� ����������� �����.
    /// </summary>
    private void DestroyBlock()
    {
        WorldManager.Instance.DestroyBlock(this);
        Destroy(gameObject);
    }
}