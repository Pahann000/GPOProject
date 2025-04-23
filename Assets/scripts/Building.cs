using UnityEngine;

/// <summary>
/// ����������� ������� ����� ��� ���� ������ � ����.
/// </summary>
/// <remarks>
/// ���������� ����� �������� � ��������� ���� ����� ������:
/// - ��������� � ��������
/// - ��������� �������������
/// - ������� ���������� � ����������
/// - ����� ������ ����������� � ����������
/// </remarks>
public abstract class Building : MonoBehaviour
{
    [Header("Base Building Settings")]
    /// <summary>
    /// ������������ �������� ������ � ����������.
    /// </summary>
    public string DisplayName;

    /// <summary>
    /// ������ ������ ��� ���������� � ���� �������������.
    /// </summary>
    public Sprite Sprite;

    /// <summary>
    /// ������������ ���������� �������� ������.
    /// </summary>
    public int MaxHealth;

    /// <summary>
    /// ������� ���������� �������� ������.
    /// </summary>
    public int CurrentHealth;

    [Header("Construction Cost")]
    /// <summary>
    /// ��������� ������������� � �������.
    /// </summary>
    public int MetalCost;

    /// <summary>
    /// ��������� ������������� � ���������.
    /// </summary>
    public int MineralCost;

    /// <summary>
    /// ��������� ������������� � ����.
    /// </summary>
    public int WaterCost;

    [Header("Requirements")]
    /// <summary>
    /// ��������� �� ������ ����������� ��� �������������.
    /// </summary>
    public bool RequiresFlatSurface = true;

    /// <summary>
    /// ��������� �� ����������� � ���������� ��� ������.
    /// </summary>
    public bool RequiresPower;

    /// <summary>
    /// ������� ��������� ������.
    /// </summary>
    public BuildingState State { get; protected set; } = BuildingState.Planned;

    /// <summary>
    /// ������� ���� ������.
    /// </summary>
    /// <param name="damage">���������� ���������� �����</param>
    /// <remarks>
    /// ��������� CurrentHealth �� ��������� ��������.
    /// ��� ���������� �������� ���� ��� ���� �������� DestroyBuilding().
    /// </remarks>
    public virtual void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DestroyBuilding();
        }
    }

    /// <summary>
    /// ���������� ������.
    /// </summary>
    /// <remarks>
    /// �������� ��������� �� Destroyed � ������� ������� ������.
    /// ����� ���� ������������� � �������� ������� ��� ���������� ����������� ������.
    /// </remarks>
    protected virtual void DestroyBuilding()
    {
        State = BuildingState.Destroyed;
        // ���������� ������� ����������
        Destroy(gameObject);
    }

    /// <summary>
    /// ��������� ������������� ������.
    /// </summary>
    /// <remarks>
    /// ��������� ������ � ��������� Operational � ��������������� �������� �� ���������.
    /// ����� ���� ������������� � �������� ������� ��� ���������� �������������� ������.
    /// </remarks>
    public virtual void CompleteConstruction()
    {
        State = BuildingState.Operational;
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// ���������� ��������� ������ ������ ����.
    /// </summary>
    /// <remarks>
    /// ������� ���������� ������. ������������ ��� ��������������� � �������� �������.
    /// </remarks>
    public virtual void Update()
    {
    }
}