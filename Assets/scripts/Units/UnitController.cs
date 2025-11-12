using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, распределяющий работу между юнитами. 
/// </summary>
public class UnitController
{
    private List<Unit> _units = new();

    /// <summary>
    /// Выбирает первого свободного юнита.
    /// </summary>
    /// <returns> экземпляр <see cref="Unit"/> </returns>
    public Unit SelectUnit()
    {
        for(int i = 0; i < _units.Count; i++)
        {
            if (_units[i].CurrentUnitWork == UnitWork.Idle)
            {
                return _units[i];
            }
        }
        return null;
    }

    /// <summary>
    /// заглушка, заставляет юнита бить блок.
    /// </summary>
    /// <param name="target"></param>
    public void CommandUnit(IDamagable target)
    {
        Unit selectedUnit = SelectUnit();
        if (!selectedUnit)
        {
            return;
        }

        selectedUnit.Target = target;
    }

    /// <summary>
    /// Спавнит юнита.
    /// </summary>
    /// <param name="unitType"> Тип юнита. </param>
    /// <param name="owner"> Владелец юнита (игрок) </param>
    /// <param name="position"> Позиция спавна юнита. </param>
    public void PlaceUnit(UnitType unitType, Player owner, Vector2 position)
    {
        //Vector2 position = new Vector2(x, y);
        //if (Physics2D.OverlapPoint(position))
        //{
        //    return null;
        //}

        GameObject newUnit = new GameObject("Unit");

        newUnit.AddComponent<SpriteRenderer>();
        newUnit.GetComponent<SpriteRenderer>().sprite = unitType.Sprite;

        newUnit.AddComponent<BoxCollider2D>();
        newUnit.GetComponent<BoxCollider2D>().size = Vector2.one;

        Rigidbody2D rb = newUnit.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        Unit unitObject = newUnit.AddComponent<Unit>();
        unitObject.unitType = unitType;
        unitObject.Owner = owner;
        unitObject.rb = rb;

        newUnit.transform.position = position;

        _units.Add(unitObject);
    }
}
