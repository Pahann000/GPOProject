using UnityEngine;

/// <summary>
/// Теплица - производственное здание для выращивания еды с потреблением воды.
/// Наследует базовую логику производства от <see cref="ProductionBuilding"/>.
/// </summary>
/// <remarks>
/// Для успешного производства требует наличия воды в указанном количестве.
/// При недостатке воды производственный цикл пропускается.
/// </remarks>
public class Greenhouse : ProductionBuilding
{
    /// <summary>
    /// Количество воды, потребляемое за один производственный цикл.
    /// </summary>
    /// <value>
    /// Положительное целое число, определяющее расход воды.
    /// </value>
    public int WaterConsumptionPerCycle;

    /// <summary>
    /// Производит пищевые ресурсы, потребляя воду.
    /// </summary>
    protected override void ProduceResource()
    {
        //if (ResourceManager.Instance.TryUseResource(ResourceType.Water, WaterConsumptionPerCycle))
        //{
        //    ResourceManager.Instance.AddResource(ResourceType.Food, ProductionAmount);
        //}
    }
}