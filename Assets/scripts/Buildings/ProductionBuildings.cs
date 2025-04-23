using UnityEngine;

/// <summary>
/// јбстрактный класс производственного здани€, создающего ресурсы с заданным интервалом.
/// Ќаследуетс€ от базового класса <see cref="Building"/>.
/// </summary>
/// <remarks>
/// –еализует базовую логику производства ресурсов, оставл€€ конкретную реализацию
/// производства на усмотрение дочерних классов.
/// </remarks>
public abstract class ProductionBuilding : Building
{
    [Header("Production Settings")]
    /// <summary>
    /// “ип ресурса, производимого зданием.
    /// </summary>
    public ResourceType ProducedResource;

    /// <summary>
    ///  оличество ресурсов, производимых за один цикл.
    /// </summary>
    public int ProductionAmount;

    /// <summary>
    /// »нтервал между циклами производства (в секундах).
    /// </summary>
    public float ProductionInterval;

    private float _lastProductionTime;

    /// <summary>
    /// ќбновл€ет состо€ние здани€ каждый кадр.
    /// </summary>
    /// <remarks>
    /// ≈сли здание находитс€ в рабочем состо€нии, провер€ет необходимость
    /// произвести новый цикл производства на основе заданного интервала.
    /// ¬ызывает базовую реализацию Update из <see cref="Building"/>.
    /// </remarks>
    private void Update()
    {
        base.Update();

        if (State == BuildingState.Operational &&
            Time.time - _lastProductionTime > ProductionInterval)
        {
            ProduceResource();
            _lastProductionTime = Time.time;
        }
    }

    /// <summary>
    /// јбстрактный метод производства ресурсов.
    /// </summary>
    /// <remarks>
    /// ƒолжен быть реализован в дочерних классах. —одержит логику
    /// создани€ и распределени€ производимых ресурсов.
    /// ¬ызываетс€ автоматически по истечении производственного интервала.
    /// </remarks>
    protected abstract void ProduceResource();
}