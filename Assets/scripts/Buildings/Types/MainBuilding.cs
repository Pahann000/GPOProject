using UnityEngine;

/// <summary>
/// Главное здание фракции: приносит владельцу пассивный доход и расширяет склады.
/// </summary>
/// <remarks>Перенос из ветки features/7_logic_of_fractions (MainBuilding). Отличия:
/// доход считается таймером в Update, а не корутиной (у нас Update здания и так
/// серверный), и работа идёт с ResourceSystem на игрока, а не с ResourceManager.Instance.</remarks>
public class MainBuilding : Building
{
    private MainBuildingData MainData => _data as MainBuildingData;

    private float _incomeTimer;
    private bool _storageApplied;

    /// <summary>
    /// Сколько выплат дохода причитается за прошедшее время.
    /// Чистая функция: при просадке кадров доход не теряется, а нулевой интервал
    /// не приводит к делению на ноль.
    /// </summary>
    public static int ComputeIncomePayouts(float elapsed, float interval)
    {
        if (interval <= 0f || elapsed <= 0f) return 0;

        return Mathf.Max(0, Mathf.FloorToInt(elapsed / interval));
    }

    public override void Update()
    {
        base.Update();

        // Ресурсы живут на сервере, поэтому доход и бонусы считает только он.
        if (!isServer) return;
        if (MainData == null) return;

        // Владелец назначается после спавна, поэтому бонус складов применяем при первой возможности.
        if (!_storageApplied) ApplyStorageBonus();

        if (State != BuildingState.Operational) return;
        if (MainData.IncomeInterval <= 0f || MainData.IncomeResources == null) return;

        _incomeTimer += Time.deltaTime;

        int payouts = ComputeIncomePayouts(_incomeTimer, MainData.IncomeInterval);
        if (payouts <= 0) return;

        if (Economy == null || Owner == null) return;

        _incomeTimer -= payouts * MainData.IncomeInterval;

        for (int i = 0; i < payouts; i++)
        {
            Economy.AddResources(Owner, MainData.IncomeResources);
        }

        Debug.Log($"[MainBuilding] {name}: доход владельцу за {payouts} интервал(ов).");
    }

    /// <summary>Увеличивает лимиты складов владельца. Повторно не применяется.</summary>
    private void ApplyStorageBonus()
    {
        if (_storageApplied) return;
        if (MainData.InitialStorageCapacity <= 0)
        {
            _storageApplied = true;
            return;
        }

        if (Economy == null || Owner == null) return;

        Economy.IncreaseAllStorage(Owner, MainData.InitialStorageCapacity);
        _storageApplied = true;

        Debug.Log($"[MainBuilding] {name}: лимиты складов увеличены на {MainData.InitialStorageCapacity}.");
    }

    protected override void DestroyBuilding()
    {
        // Бонус складов снимаем: иначе он остался бы у игрока навсегда.
        if (_storageApplied && MainData != null && MainData.InitialStorageCapacity > 0
            && Economy != null && Owner != null)
        {
            Economy.DecreaseAllStorage(Owner, MainData.InitialStorageCapacity);
            _storageApplied = false;
        }

        base.DestroyBuilding();
    }
}
