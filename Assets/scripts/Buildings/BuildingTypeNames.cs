/// <summary>
/// Типы зданий: по этому значению <see cref="BuilderSystem"/> выбирает префаб.
/// Значения заданы явно: перечисление уходит по сети (CmdPlaceBuilding,
/// SyncVar у Building), и вставка нового значения в середину списка сломала бы
/// совместимость протокола.
/// </summary>
public enum BuildingTypeNames
{
    House = 0,

    /// <summary>Главное здание фракции: пассивный доход и склады.</summary>
    MainBuilding = 1,

    /// <summary>Здание-бонус: расширяет лимит хранения ресурса.</summary>
    ExtendsBuilding = 2,

    /// <summary>Шахта: добывает минералы из камня.</summary>
    Mine = 3,

    /// <summary>Теплица: перерабатывает лёд в пищу.</summary>
    Greenhouse = 4,

    /// <summary>Электростанция: производит энергию.</summary>
    PowerPlant = 5,

    /// <summary>Турель: оборонительное сооружение.</summary>
    Turret = 6,

    /// <summary>Хранилище конкретного ресурса.</summary>
    StorageBuilding = 7,

    /// <summary>Жилой модуль: увеличивает максимальное население.</summary>
    LivingModule = 8,

    /// <summary>Научная лаборатория: исследования.</summary>
    ResearchLab = 9,
}
