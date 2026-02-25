using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, распределяющий работу между юнитами. 
/// </summary>
public class UnitController
{
    private List<Unit> _units = new();

    private List<Task> pendingTasks = new List<Task>();
    private List<ActiveTask> activeTasks = new List<ActiveTask>();

    public void DistributeTasks()
    {
        for (int i = pendingTasks.Count - 1; i >= 0; i--)
        {
            Task task = pendingTasks[i];
            Unit bestUnit = null;
            float bestDist = float.MaxValue;

            foreach (var unit in _units)
            {
                // Подходит по типу, жив и свободен
                if (unit.unitType.UnitTypeName == task.RequiredUnitType &&
                    unit.CurrentUnitWork == UnitWork.Idle)
                {
                    float dist = Vector3.Distance(unit.transform.position, task.TargetPosition);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestUnit = unit;
                    }
                }
            }

            if (bestUnit != null)
            {
                AssignTask(bestUnit, task);
                pendingTasks.RemoveAt(i);
            }
        }
    }

    //TODO: нормально добавить методы постройки и атаки юниту, а не то что сейчас
    private void AssignTask(Unit unit, Task task)
    {
        activeTasks.Add(new ActiveTask { Unit = unit, Task = task });

        if (task.Target is IDamagable damagable)
        {
            unit.Target = damagable;
        }
        //else if (task.Target is Building building)
        //{
        //    unit.Build(building);
        //}
        else
        {
            Debug.LogError("Unknown task target type");
        }
    }

    // методы для добавления задач
    public void AddTask(IDamagable target, UnitTypeName requiredType)
    {
        pendingTasks.Add(new Task(target, requiredType));
    }

    public void AddTask(Building target)
    {
        pendingTasks.Add(new Task(target, UnitTypeName.Worker));
    }

    public void TaskCompleted(Unit unit)
    {
        for (int i = 0; i < activeTasks.Count; i++)
        {
            if (activeTasks[i].Unit == unit)
            {
                activeTasks.RemoveAt(i);
                break;
            }
        }
    }

    public void CheckActiveTasks() 
    {
        for (int i = activeTasks.Count - 1; i >= 0; i--)
        {
            var active = activeTasks[i];
            Unit unit = active.Unit;
            Task task = active.Task;

            // Если юнит мёртв – освобождаем задачу (если цель ещё актуальна)
            if (unit.CurrentUnitWork == UnitWork.Dead)
            {
                if (task.IsTargetValid())
                    pendingTasks.Add(task);
                activeTasks.RemoveAt(i);
                continue;
            }

            // Если цель стала невалидной (уничтожена/построена) – отменяем задачу
            if (!task.IsTargetValid())
            {
                //unit.StopCurrentTask();
                activeTasks.RemoveAt(i);
                continue;
            }

            // Если юнит почему-то бездействует, а задача не выполнена – перепланируем
            if (unit.CurrentUnitWork == UnitWork.Idle && task.IsTargetValid())
            {
                pendingTasks.Add(task);
                activeTasks.RemoveAt(i);
            }
        }
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
        unitObject.EndTask += TaskCompleted;

        newUnit.transform.position = position;

        _units.Add(unitObject);
    }
}
