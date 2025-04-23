using UnityEngine;

/// <summary>
/// ∆илой модуль - здание, увеличивающее максимальное население колонии.
/// Ќаследуетс€ от базового класса <see cref="Building"/>.
/// </summary>
/// <remarks>
/// ѕри завершении строительства увеличивает максимальное население,
/// при уничтожении - уменьшает его.
/// </remarks>
public class LivingModule : Building
{
    /// <summary>
    /// ¬еличина увеличени€ максимального населени€ колонии.
    /// </summary>
    /// <value>
    /// ѕоложительное целое число, добавл€емое к максимальному населению.
    /// </value>
    public int PopulationIncrease;

    /// <summary>
    /// «авершает строительство жилого модул€.
    /// </summary>
    /// <remarks>
    /// ¬ызывает базовую реализацию, затем увеличивает максимальное население колонии
    /// на значение <see cref="PopulationIncrease"/> через <see cref="ColonyManager"/>.
    /// </remarks>
    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        //ColonyManager.Instance.MaxPopulation += PopulationIncrease;
    }

    /// <summary>
    /// ”ничтожает жилой модуль.
    /// </summary>
    /// <remarks>
    /// ”меньшает максимальное население колонии на значение <see cref="PopulationIncrease"/>,
    /// затем вызывает базовую реализацию уничтожени€ здани€.
    /// </remarks>
    protected override void DestroyBuilding()
    {
        //ColonyManager.Instance.MaxPopulation -= PopulationIncrease;
        base.DestroyBuilding();
    }
}