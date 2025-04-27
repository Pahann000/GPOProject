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
            var destinationSetter = _selectedUnit.GetComponent<AIDestinationSetter>();
            if (destinationSetter != null)
            {
                destinationSetter.target = block.transform;
                Debug.Log($"���� ������� ������� ��������� {block.BlockType.Name}");
            }
            else
            {
                Debug.LogError("AIDestinationSetter �� ������ � ���������� �����.");
            }
        }
    }

    private void SelectUnit(Unit unit)
    {
        // ������� ��������� � ����������� �����
        if (_selectedUnit != null)
        {
            unit.Deselect();
        }

        _selectedUnit = unit;
        Debug.Log($"������ ����: {unit.name}");

        unit.Select();
    }
}