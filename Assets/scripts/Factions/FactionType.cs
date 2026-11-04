/// <summary>
/// Фракции игры. Значения заданы явно: перечисление уходит по сети
/// (SyncVar у <see cref="Player"/>, <see cref="Building"/>, <see cref="Unit"/>
/// и <see cref="FactionBase"/>), и вставка нового значения в середину списка
/// сломала бы совместимость протокола.
/// </summary>
public enum FactionType
{
    /// <summary>Фракция ещё не назначена (сервер её выберет при подключении).</summary>
    None = 0,

    /// <summary>Люди — база на поверхности планеты.</summary>
    Humans = 1,

    /// <summary>Марсиане — база под землёй, в вырубленной пещере.</summary>
    Martians = 2,
}
