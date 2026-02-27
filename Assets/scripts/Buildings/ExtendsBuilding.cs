using UnityEngine;

/// <summary>
/// Здание, которое расширяет возможности колонии (лимиты, исследования и т.д.).
/// </summary>
public class ExtendsBuilding : Building
{
    private ExtendsBuildingData ExtendsData => _data as ExtendsBuildingData;

    protected override void Start()
    {
        base.Start();
        if (ExtendsData == null)
        {
            Debug.LogError($"ExtendsBuilding {name} имеет неверный тип данных!");
            return;
        }
        ApplyBonus();
    }

    /// <summary>
    /// Применить бонус при строительстве.
    /// </summary>
    protected virtual void ApplyBonus()
    {
        switch (ExtendsData.ExtendType)
        {
            case ExtendType.PopulationLimit:
                ResourceManager.Instance.IncreasePopulationLimit(ExtendsData.BonusValue);
                break;
            case ExtendType.ResourceStorage:
                ResourceManager.Instance.IncreaseStorage(ExtendsData.StoredResource, ExtendsData.BonusValue);
                break;
            case ExtendType.ResearchUnlock:
                // Предположим, есть ResearchManager, которому передаём список проектов
                //ResearchManager.Instance.RegisterLab(this, ExtendsData.AvailableProjects);
                break;
            case ExtendType.DefenseBonus:
                // Пример: увеличить радиус действия ближайших турелей
                ApplyDefenseBonus();
                break;
        }
    }

    protected virtual void ApplyDefenseBonus()
    {
        // Логика увеличения радиуса/урона для турелей в радиусе
    }

    protected override void DestroyBuilding()
    {
        RemoveBonus();
        base.DestroyBuilding();
    }

    /// <summary>
    /// Убрать бонус при разрушении.
    /// </summary>
    protected virtual void RemoveBonus()
    {
        switch (ExtendsData.ExtendType)
        {
            case ExtendType.PopulationLimit:
                ResourceManager.Instance.DecreasePopulationLimit(ExtendsData.BonusValue);
                break;
            case ExtendType.ResourceStorage:
                ResourceManager.Instance.DecreaseStorage(ExtendsData.StoredResource, ExtendsData.BonusValue);
                break;
            case ExtendType.ResearchUnlock:
                //ResearchManager.Instance.UnregisterLab(this);
                break;
            case ExtendType.DefenseBonus:
                RemoveDefenseBonus();
                break;
        }
    }

    protected virtual void RemoveDefenseBonus()
    {
        // Отмена бонуса
    }
}