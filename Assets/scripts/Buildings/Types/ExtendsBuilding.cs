using UnityEngine;

/// <summary>
/// Здание-бонус: расширяет лимит хранения выбранного ресурса у владельца.
/// </summary>
/// <remarks>Перенос из ветки features/7_logic_of_fractions (ExtendsBuilding).
/// Из их типов бонусов перенесён только склад: системы населения и исследований
/// в нашей ветке отсутствуют.</remarks>
public class ExtendsBuilding : Building
{
    private ExtendsBuildingData ExtendsData => _data as ExtendsBuildingData;

    private bool _bonusApplied;

    public override void Update()
    {
        base.Update();

        if (!isServer) return;

        // Владелец назначается после спавна — применяем бонус, когда он уже есть.
        if (!_bonusApplied) ApplyBonus();
    }

    private void ApplyBonus()
    {
        if (_bonusApplied || !CanApply()) return;

        Economy.IncreaseStorage(Owner, new ResourcePair(ExtendsData.StoredResource, ExtendsData.BonusValue));
        _bonusApplied = true;

        Debug.Log($"[ExtendsBuilding] {name}: лимит {ExtendsData.StoredResource} увеличен на {ExtendsData.BonusValue}.");
    }

    protected override void DestroyBuilding()
    {
        if (_bonusApplied && CanApply())
        {
            Economy.DecreaseStorage(Owner, new ResourcePair(ExtendsData.StoredResource, ExtendsData.BonusValue));
            _bonusApplied = false;
        }

        base.DestroyBuilding();
    }

    /// <summary>Данные, владелец и экономика на месте.</summary>
    private bool CanApply()
    {
        return ExtendsData != null
            && ExtendsData.BonusValue > 0
            && Economy != null
            && Owner != null;
    }
}
