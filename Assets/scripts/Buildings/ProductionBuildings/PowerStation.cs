using UnityEngine;

/// <summary>
/// Электростанция - производственное здание, генерирующее энергию.
/// Наследует базовую логику производства от <see cref="ProductionBuilding"/>.
/// </summary>
/// <remarks>
/// Производит фиксированное количество энергии через заданные интервалы времени.
/// </remarks>
public class PowerPlant : ProductionBuilding
{
    /// <summary>
    /// Количество энергии, производимой за один цикл.
    /// </summary>
    /// <value>
    /// Положительное целое число, определяющее выход энергии.
    /// </value>
    public int EnergyOutput;

    /// <summary>
    /// Реализация производственного цикла для генерации энергии.
    /// </summary>
    /// <remarks>
    /// Вызывается автоматически через заданные интервалы <see cref="ProductionBuilding.ProductionInterval"/>.
    /// Добавляет указанное количество <see cref="EnergyOutput"/> в общий пул ресурсов.
    /// </remarks>
    protected override void ProduceResource()
    {
        // Добавляем энергию в общий пул
        //ResourceManager.Instance.AddResource(ResourceType.Energy, EnergyOutput);
    }
}