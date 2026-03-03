using UnityEngine;

[CreateAssetMenu(fileName = "Meteor", menuName = "Incidents/Meteor")]
public class MeteorIncident : GameIncident
{
    // TODO: Придумать реализацию метеорита.

    // Мысль: Выбираем случаную позицию, ищем объекты с интерфейсом IDamagable и атакуем по радиусу от взрыва (с затуханием).
    // Нужен визуальный эффект.

    public override void Execute(GameKernel kernel)
    {
        Debug.Log("Метеорит упал в какой-то точке.");
    }
}