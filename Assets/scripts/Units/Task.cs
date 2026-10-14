using UnityEngine;

/// <summary>
/// Задача для юнита: что делать и какому юниту это по силам.
/// </summary>
/// <remarks>
/// Перенос из ветки features/9_unit_work_spread_system (коммит 2048b26).
/// Отличия: цель типизирована как <see cref="IDamagable"/>, у задачи есть владелец,
/// чтобы задачи разных игроков не смешивались в одну очередь.
/// </remarks>
public class Task
{
    /// <summary>Кто поставил задачу: юниты других игроков её не получат.</summary>
    public Player Owner { get; }

    /// <summary>Цель задачи (блок, чужой юнит).</summary>
    public IDamagable Target { get; }

    /// <summary>Тип юнита, который может выполнить задачу.</summary>
    public UnitTypeName RequiredUnitType { get; }

    /// <summary>Позиция цели: по ней ищется ближайший свободный юнит.</summary>
    public Vector2 TargetPosition { get; }

    public Task(Player owner, IDamagable target, UnitTypeName requiredUnitType)
    {
        Owner = owner;
        Target = target;
        RequiredUnitType = requiredUnitType;

        // Блок — обычный класс с координатами, юнит — компонент на сцене.
        TargetPosition = target is Component component
            ? component.transform.position
            : new Vector2(target.X, target.Y);
    }

    /// <summary>Короткое описание цели для логов.</summary>
    public string TargetLabel => $"{Target?.GetType().Name} ({Target?.X},{Target?.Y})";

    /// <summary>
    /// Цель ещё актуальна. Уничтоженный объект Unity сравнивается с null,
    /// поэтому проверяем и его, и здоровье.
    /// </summary>
    public bool IsTargetValid()
    {
        if (Target is Object unityObject && unityObject == null) return false;

        return Target.CurrentHealth > 0;
    }
}

/// <summary>
/// Задача, которая уже отдана конкретному юниту.
/// </summary>
public class ActiveTask
{
    public Unit Unit;
    public Task Task;
}
