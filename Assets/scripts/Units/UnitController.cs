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
        Unit newUnit = WorldManager.Instance.PlaceUnit(unitType, position, owner);
        _units.Add(newUnit);
    }
}
