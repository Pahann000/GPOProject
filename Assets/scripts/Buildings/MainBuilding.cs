using UnityEngine;
using System.Collections;

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
        if (MainData.InitialStorageCapacity > 0)
        {
            // Предположим, ResourceManager может увеличить лимиты для всех ресурсов
            ResourceManager.Instance.IncreaseAllStorage(MainData.InitialStorageCapacity);
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
            _resourceManager.AddResources(MainData.IncomeResources);
        }
    }

    protected override void DestroyBuilding()
    {
        if (_incomeCoroutine != null)
            StopCoroutine(_incomeCoroutine);

        // Отменяем увеличение лимитов
        if (MainData.InitialStorageCapacity > 0)
        {
            ResourceManager.Instance.DecreaseAllStorage(MainData.InitialStorageCapacity);
        }

        //// Уведомляем GameManager о поражении
        //GameManager.Instance.GameOver(Player); // предполагаем, что Player – владелец

        base.DestroyBuilding();
    }
}