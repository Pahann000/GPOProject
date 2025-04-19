using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Класс игрока.
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>
    /// Атлас со всеми видами юнитов.
    /// </summary>
    public UnitAtlas unitAtlas;

    /// <summary>
    /// Список всех принадлежащих игроку юнитов.
    /// </summary>
    private List<Unit> _units = new List<Unit>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //TODO: Вынести создание юнитов в отдельную функцию или перенсти её в блок для создания юнитов.
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

    //TODO: сделать нормальное выделение, и вообще работу игрока с блоками
    /// <summary>
    /// выделяет блок(заглушка заставляющая первого юнита уничтожить блок).
    /// </summary>
    /// <param name="position">Позиция выделяемого блока.</param>
    private void SelectBlock(Vector2 position)
    {
        Collider2D collider = Physics2D.OverlapPoint(position);
        Debug.Log($"нажатие сделано на позиции{position}");
        if (collider)
        {
            Block selectedBlock = collider.GetComponentInParent<Block>();
            Debug.Log($"блок найден:{selectedBlock.BlockType.Name}");
            if (selectedBlock != null)
            {
                SetDestoroyBlock(selectedBlock);
            }
        }
    }

    /// <summary>
    /// помечает блок как для уничтожения(заглушка)
    /// </summary>
    /// <param name="blockToDestroy"></param>
    public void SetDestoroyBlock(Block blockToDestroy)
    {
        _units[0].Target = blockToDestroy.transform.gameObject;
    }
}
