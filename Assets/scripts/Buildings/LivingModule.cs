using UnityEngine;

/// <summary>
/// Жилой модуль - здание, увеличивающее максимальное население колонии.
/// Наследуется от базового класса <see cref="Building"/>.
/// </summary>
/// <remarks>
/// При завершении строительства увеличивает максимальное население,
/// при уничтожении - уменьшает его.
/// </remarks>
public class LivingModule : Building
{
    /// <summary>
    /// Величина увеличения максимального населения колонии.
    /// </summary>
    /// <value>
    /// Положительное целое число, добавляемое к максимальному населению.
    /// </value>
    public int PopulationIncrease;

    /// <summary>
    /// Уничтожает жилой модуль.
    /// </summary>
    /// <remarks>
    /// Уменьшает максимальное население колонии на значение <see cref="PopulationIncrease"/>,
    /// затем вызывает базовую реализацию уничтожения здания.
    /// </remarks>
    protected override void DestroyBuilding()
    {
        //ColonyManager.Instance.MaxPopulation -= PopulationIncrease;
        base.DestroyBuilding();
    }
}