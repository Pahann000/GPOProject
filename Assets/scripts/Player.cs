using Pathfinding;
using UnityEngine;

public class Player : MonoBehaviour
{
    public UnitAtlas unitAtlas;

    private List<Unit> _units = new List<Unit>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

    private void SelectBlock(Vector2 position)
    {
        Collider2D collider = Physics2D.OverlapPoint(position);
        Debug.Log($"íàæàòèå ñäåëàíî íà ïîçèöèè{position}");
        if (collider)
        {
            Block selectedBlock = collider.GetComponentInParent<Block>();
            Debug.Log($"áëîê íàéäåí:{selectedBlock.BlockType.Name}");
            if (selectedBlock != null)
            {
                SetDestoroyBlock(selectedBlock);
            }
        }
    }

    public void SetDestoroyBlock(Block blockToDestroy)
    {
        _units[0].SetTarget(blockToDestroy.transform);
    }
}