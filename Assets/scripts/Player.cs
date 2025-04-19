using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ����� ������.
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>
    /// ����� �� ����� ������ ������.
    /// </summary>
    public UnitAtlas unitAtlas;

    /// <summary>
    /// ������ ���� ������������� ������ ������.
    /// </summary>
    private List<Unit> _units = new List<Unit>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //TODO: ������� �������� ������ � ��������� ������� ��� �������� � � ���� ��� �������� ������.
        Unit newUnit = WorldManager.Instance.PlaceUnit(unitAtlas.Miner, 0, 200);
        _units.Add(newUnit);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
            SelectBlock(new Vector2(mousePos.origin.x, mousePos.origin.y));
        }
    }

    //TODO: ������� ���������� ���������, � ������ ������ ������ � �������
    /// <summary>
    /// �������� ����(�������� ������������ ������� ����� ���������� ����).
    /// </summary>
    /// <param name="position">������� ����������� �����.</param>
    private void SelectBlock(Vector2 position)
    {
        Collider2D collider = Physics2D.OverlapPoint(position);
        Debug.Log($"������� ������� �� �������{position}");
        if (collider)
        {
            Block selectedBlock = collider.GetComponentInParent<Block>();
            Debug.Log($"���� ������:{selectedBlock.BlockType.Name}");
            if (selectedBlock != null)
            {
                SetDestoroyBlock(selectedBlock);
            }
        }
    }

    /// <summary>
    /// �������� ���� ��� ��� �����������(��������)
    /// </summary>
    /// <param name="blockToDestroy"></param>
    public void SetDestoroyBlock(Block blockToDestroy)
    {
        _units[0].Target = blockToDestroy.transform.gameObject;
    }
}
