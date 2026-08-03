using System.Collections;
using UnityEngine;

/// <summary>
/// Главное здание колонии. При разрушении игра заканчивается.
/// </summary>
public class MainBuilding : Building
{
    private MainBuildingData MainData => _data as MainBuildingData;
    private Coroutine _incomeCoroutine;

    protected override void Start()
    {
        base.Start();
        if (MainData == null)
        {
            Debug.LogError($"MainBuilding {name} имеет неверный тип данных!");
            return;
        }

        // Увеличиваем лимиты хранилища, если задано
        if (MainData.InitialStorageCapacity > 0 && _resourceSystem != null && _data.Owner != null)
        {
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                _resourceSystem.IncreaseStorage(_data.Owner, new ResourcePair(type, MainData.InitialStorageCapacity));
            }
        }

        // Запускаем пассивный доход
        if (MainData.IncomeInterval > 0)
        {
            _incomeCoroutine = StartCoroutine(GenerateIncome());
        }
    }

    private IEnumerator GenerateIncome()
    {
        while (State == BuildingState.Operational)
        {
            yield return new WaitForSeconds(MainData.IncomeInterval);
            if (_resourceSystem != null && _data?.Owner != null && MainData.IncomeResources != null)
            {
                _resourceSystem.AddResources(_data.Owner, MainData.IncomeResources);
            }
        }
    }

    protected override void DestroyBuilding()
    {
        if (_incomeCoroutine != null)
            StopCoroutine(_incomeCoroutine);

        // Отменяем увеличение лимитов
        if (MainData != null && MainData.InitialStorageCapacity > 0 && _resourceSystem != null && _data.Owner != null)
        {
            foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
            {
                _resourceSystem.DecreaseStorage(_data.Owner, new ResourcePair(type, MainData.InitialStorageCapacity));
            }
        }
        
        base.DestroyBuilding();
    }
}