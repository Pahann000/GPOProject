using System.Collections.Generic;
using UnityEngine;

public class UnitController
{
    private List<Unit> _units = new();
    private int _currentUnitIndex;

    /// <summary>
    /// TODO: сюда выбор юнита надо адекватный
    /// </summary>
    public void SelectUnit()
    {
        for(int i = 0; i < _units.Count; i++)
        {
            if (_units[_currentUnitIndex].CurrentUnitWork == UnitWork.Idle)
            {
                _currentUnitIndex = i;
                break;
            }
        }
    }

    /// <summary>
    /// заглушка, заставляет юнита бить блок
    /// </summary>
    /// <param name="target"></param>
    public void CommandUnit(GameObject target)
    {
        SelectUnit();
        _units[_currentUnitIndex].Target = target;
    }

    public void PlaceUnit(UnitType unitType, Player owner, Vector2 position)
    {
        //Vector2 position = new Vector2(x, y);
        //if (Physics2D.OverlapPoint(position))
        //{
        //    return null;
        //}

        GameObject newUnit = new GameObject();

        newUnit.AddComponent<SpriteRenderer>();
        newUnit.GetComponent<SpriteRenderer>().sprite = unitType.Sprite;

        newUnit.AddComponent<BoxCollider2D>();
        newUnit.GetComponent<BoxCollider2D>().size = Vector2.one;

        newUnit.AddComponent<Rigidbody2D>();
        newUnit.GetComponent<Rigidbody2D>().freezeRotation = true;

        newUnit.AddComponent<Unit>();
        newUnit.GetComponent<Unit>().unitType = unitType;
        newUnit.GetComponent<Unit>().Owner = owner;

        newUnit.transform.position = position;

        _units.Add(newUnit.GetComponent<Unit>());
    }
}
