using UnityEngine;

/// <summary>
/// Данные здания-бонуса: расширяет лимит хранения выбранного ресурса.
/// </summary>
/// <remarks>Перенос из ветки features/7_logic_of_fractions (ExtendsBuildingData).
/// Бонусы населения и исследований не переносились: таких систем в нашей ветке нет.</remarks>
[CreateAssetMenu(menuName = "Buildings/Extends Building", fileName = "ExtendsBuildingData")]
public class ExtendsBuildingData : BuildingData
{
    [Tooltip("Лимит какого ресурса расширяется.")]
    public ResourceType StoredResource = ResourceType.Minerals;

    [Tooltip("На сколько увеличивается лимит хранения.")]
    public int BonusValue = 100;
}
