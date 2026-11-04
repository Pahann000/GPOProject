using UnityEngine;

/// <summary>
/// Атлас фракций: связывает <see cref="FactionType"/> с его описанием.
/// Ассет лежит в Resources, поэтому и сервер, и клиент могут получить его
/// без ссылок в сцене или префабах (<see cref="Cached"/>).
/// </summary>
[CreateAssetMenu(menuName = "Factions/Faction Atlas", fileName = "FactionAtlas")]
public class FactionAtlas : ScriptableObject
{
    /// <summary>Имя ассета в папке Resources.</summary>
    public const string ResourceName = "FactionAtlas";

    [Tooltip("Фракция людей.")]
    public FactionDefinition Humans;

    [Tooltip("Фракция марсиан.")]
    public FactionDefinition Martians;

    private static FactionAtlas _cached;

    /// <summary>
    /// Общий атлас фракций, загруженный из Resources (с кэшированием).
    /// Может быть null, если ассета нет — вызывающая сторона обязана это учесть.
    /// </summary>
    public static FactionAtlas Cached
    {
        get
        {
            if (_cached == null)
            {
                _cached = Resources.Load<FactionAtlas>(ResourceName);
            }

            return _cached;
        }
    }

    /// <summary>Сбрасывает кэш (нужно после подмены ассета в тестах).</summary>
    public static void ResetCache()
    {
        _cached = null;
    }

    /// <summary>Описание фракции или null для <see cref="FactionType.None"/>.</summary>
    public FactionDefinition Get(FactionType faction)
    {
        switch (faction)
        {
            case FactionType.Humans:
                return Humans;
            case FactionType.Martians:
                return Martians;
            default:
                return null;
        }
    }

    /// <summary>Спрайт здания фракции из общего атласа (null-safe).</summary>
    public static Sprite GetBuildingSprite(FactionType faction, BuildingTypeNames type)
    {
        FactionAtlas atlas = Cached;
        if (atlas == null) return null;

        FactionDefinition definition = atlas.Get(faction);
        return definition != null ? definition.GetBuildingSprite(type) : null;
    }

    /// <summary>Описание фракции из общего атласа (null-safe).</summary>
    public static FactionDefinition GetDefinition(FactionType faction)
    {
        FactionAtlas atlas = Cached;
        return atlas != null ? atlas.Get(faction) : null;
    }
}
