using UnityEngine;

/// <summary>
/// «дание, предоставл€ющее пассивные бонусы (склады, жилые модули).
/// </summary>
public class ExtendsBuilding : Building
{
    private ExtendsBuildingData ExtendsData => _data as ExtendsBuildingData;

    protected override void Start()
    {
        base.Start();
        if (ExtendsData == null)
        {
            Debug.LogError($"ExtendsBuilding {name} не содержит ExtendsBuildingData!");
            return;
        }
        ApplyBonus();
    }

    protected virtual void ApplyBonus()
    {
        if (_resourceSystem == null || _data?.Owner == null) return;

        switch (ExtendsData.ExtendType)
        {
            case ExtendType.ResourceStorage:
                _resourceSystem.IncreaseStorage(_data.Owner, new ResourcePair(ExtendsData.StoredResource, ExtendsData.BonusValue));
                break;
            case ExtendType.PopulationLimit:
                // TODO: ¬ызвать метод увеличени€ лимита населени€ в будущем классе Colony/Player
                break;
        }
    }

    protected override void DestroyBuilding()
    {
        RemoveBonus();
        base.DestroyBuilding();
    }

    protected virtual void RemoveBonus()
    {
        if (_resourceSystem == null || _data?.Owner == null) return;

        switch (ExtendsData.ExtendType)
        {
            case ExtendType.ResourceStorage:
                _resourceSystem.DecreaseStorage(_data.Owner, new ResourcePair(ExtendsData.StoredResource, ExtendsData.BonusValue));
                break;
            case ExtendType.PopulationLimit:
                break;
        }
    }
}