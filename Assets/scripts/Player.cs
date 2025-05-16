using Pathfinding;
using UnityEngine;

public class Player : MonoBehaviour
{
    public UnitAtlas unitAtlas;
    private Unit _selectedUnit;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleSelection();
        }
    }

    private void HandleSelection()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D hit = Physics2D.OverlapPoint(mousePos);

        if (hit == null) return;

        // ���� �������� �� ����� - �������� ���
        if (hit.TryGetComponent<Unit>(out var unit))
        {
            SelectUnit(unit);
            return;
        }

        // ���� �������� �� ����� � ���� ��������� ���� - ���������� ���������
        if (hit.TryGetComponent<Block>(out var block) && _selectedUnit != null)
        {
            _selectedUnit.SetTarget(block.transform);
            Debug.Log($"���� ������� ������� ��������� {block.BlockType.Name}");
        }
    }

    private void SelectUnit(Unit unit)
    {
        _selectedUnit = unit;
        Debug.Log($"������ ����: {unit.name}");
    }
}