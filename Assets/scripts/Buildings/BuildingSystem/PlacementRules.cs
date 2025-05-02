using UnityEngine;

/// <summary>
/// ����������� ������� ����� ��� ������ ���������� ������
/// </summary>
public abstract class PlacementRule : ScriptableObject
{
    /// <summary>
    /// ���������, ������������� �� ������� ������� ����������
    /// </summary>
    /// <param name="position">������� ��� ��������</param>
    /// <returns>True, ���� ������� ������������� �������</returns>
    public abstract bool IsSatisfied(Vector2 position);
}

/// <summary>
/// ���������� ���������� ������� ���������� "����� � �����"
/// </summary>
[CreateAssetMenu(menuName = "Buildings/Rules/Near Water")]
public class NearWaterRule : PlacementRule
{
    [Tooltip("������ ������ ������ ��������")]
    [SerializeField] private float _searchRadius = 3f;

    [Tooltip("����, �� ������� ��������� ������ �������")]
    [SerializeField] private LayerMask _waterLayer;

    /// <summary>
    /// ���������, ��������� �� ������� ����� � �����
    /// </summary>
    public override bool IsSatisfied(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, _searchRadius, _waterLayer);
    }
}

