using UnityEngine;

/// <summary>
/// Данные главного здания фракции: пассивный доход владельцу и стартовая ёмкость складов.
/// </summary>
/// <remarks>Перенос из ветки features/7_logic_of_fractions (MainBuildingData).</remarks>
[CreateAssetMenu(menuName = "Buildings/Main Building", fileName = "MainBuildingData")]
public class MainBuildingData : BuildingData
{
    [Header("Доход главного здания")]
    [Tooltip("Интервал выдачи дохода в секундах (0 или меньше — дохода нет).")]
    public float IncomeInterval = 10f;

    [Tooltip("Что и сколько владелец получает за один интервал.")]
    public ResourcePair[] IncomeResources;

    [Header("Склады")]
    [Tooltip("На сколько увеличивается лимит каждого ресурса при постройке (0 — без бонуса).")]
    public int InitialStorageCapacity;
}
