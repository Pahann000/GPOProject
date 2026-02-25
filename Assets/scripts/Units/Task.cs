using UnityEngine;

// Класс задачи
public class Task
{
    public object Target { get; private set; }
    public UnitTypeName RequiredUnitType { get; private set; }
    public Vector2 TargetPosition { get; private set; }

    public Task(IDamagable target, UnitTypeName requiredType)
    {
        Target = target;
        RequiredUnitType = requiredType;
        TargetPosition = new Vector2(target.X, target.Y);
    }

    public Task(Building target, UnitTypeName requiredType)
    {
        Target = target;
        RequiredUnitType = requiredType;
        TargetPosition = target.transform.position;
    }

    public bool IsTargetValid()
    {
        if (Target == null) return false;
        if (Target is IDamagable d) return d.CurrentHealth > 0;
        if (Target is Building b) return b.State == BuildingState.Planned;
        return false;
    }
}

// Активная задача
public class ActiveTask
{
    public Unit Unit;
    public Task Task;
}
