using UnityEngine;

/// <summary>
/// Жилой модуль — здание, увеличивающее максимальное население колонии.
/// Наследуется от базового класса <see cref="Building"/>.
/// </summary>
/// <remarks>
/// При уничтожении уменьшает максимальное население.
/// Увеличение пока не реализовано: системы населения в проекте нет
/// (раньше здесь был закомментированный вызов несуществующего ColonyManager).
/// TODO: подключить систему населения и увеличивать лимит при постройке.
/// </remarks>
public class LivingModule : Building
{
    /// <summary>
    /// Величина увеличения максимального населения колонии.
    /// </summary>
    public int PopulationIncrease;

    /// <summary>
    /// Уничтожает жилой модуль.
    /// </summary>
    protected override void DestroyBuilding()
    {
        if (!isServer) return;

        // TODO: уменьшать максимальное население, когда появится система населения.

        base.DestroyBuilding();
    }
}
