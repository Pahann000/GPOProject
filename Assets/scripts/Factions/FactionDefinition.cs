using UnityEngine;

/// <summary>
/// Графика юнита одной фракции: кадры анимации и иконка.
/// </summary>
[System.Serializable]
public class FactionUnitVisuals
{
    [Tooltip("Тип юнита, к которому относится эта графика.")]
    public UnitTypeName Type = UnitTypeName.Worker;

    [Tooltip("Кадры циклической анимации. Один кадр — статичный спрайт.")]
    public Sprite[] Frames;

    [Tooltip("Иконка для UI (если не задана, берётся первый кадр).")]
    public Sprite Icon;
}

/// <summary>
/// Графика здания одной фракции.
/// </summary>
[System.Serializable]
public class FactionBuildingVisuals
{
    [Tooltip("Тип здания.")]
    public BuildingTypeNames Type = BuildingTypeNames.House;

    [Tooltip("Спрайт здания этой фракции.")]
    public Sprite Sprite;
}

/// <summary>
/// Описание фракции: название, цвета, расположение базы и собственные
/// текстуры с анимациями юнитов и зданий.
/// </summary>
/// <remarks>
/// У каждой фракции свой ассет, поэтому «свои текстуры» — это данные, а не
/// код: достаточно подменить спрайты в ассете, чтобы фракция выглядела иначе.
/// Анимация тоже настраивается здесь (частота кадров, покачивание, пульсация).
/// </remarks>
[CreateAssetMenu(menuName = "Factions/Faction Definition", fileName = "FactionDefinition")]
public class FactionDefinition : ScriptableObject
{
    [Header("Идентификация")]
    public FactionType Faction = FactionType.Humans;

    [Tooltip("Отображаемое название (показывается в HUD).")]
    public string DisplayName = "Люди";

    [Header("Цвета")]
    public Color PrimaryColor = new Color(0.35f, 0.6f, 1f, 1f);

    [Tooltip("Второй цвет: используется для пульсации.")]
    public Color SecondaryColor = new Color(1f, 1f, 1f, 1f);

    [Header("Расположение базы")]
    [Tooltip("True — база на поверхности, False — в вырубленной пещере под землёй.")]
    public bool SpawnsOnSurface = true;

    [Tooltip("Глубина пещеры в блоках ниже поверхности (только для подземной фракции).")]
    public int UndergroundDepth = 16;

    [Tooltip("Половина ширины расчищаемой площадки в блоках.")]
    public int ClearHalfWidth = 6;

    [Tooltip("Высота расчищаемого пространства в блоках.")]
    public int ClearHeight = 4;

    [Tooltip("Доля ширины карты, на которой стоит база фракции (0..1).")]
    [Range(0.05f, 0.95f)]
    public float BaseMapFraction = 0.25f;

    [Tooltip("Чем закрывается пол под базой (блок должен быть пригоден для строительства).")]
    public BlockType FloorBlock = BlockType.Stone;

    [Tooltip("Чем закрывается свод пещеры (используется только подземной фракцией).")]
    public BlockType CeilingBlock = BlockType.Stone;

    [Header("Графика юнитов")]
    public FactionUnitVisuals[] UnitVisuals;

    [Header("Графика зданий")]
    public FactionBuildingVisuals[] BuildingVisuals;

    [Tooltip("Спрайт базы/флага фракции.")]
    public Sprite BaseSprite;

    [Header("Стартовые ресурсы")]
    [Tooltip("Что фракция получает при подключении игрока. Нужно, чтобы сразу можно было строить.")]
    public ResourcePair[] StartingResources;

    [Header("Анимация")]
    [Tooltip("Скорость смены кадров анимации юнитов, кадров в секунду.")]
    public float FramesPerSecond = 4f;

    [Tooltip("Амплитуда покачивания зданий и базы в мировых единицах (юниты не качаются: их позицию ведёт сеть).")]
    public float BobAmplitude = 0.05f;

    [Tooltip("Частота покачивания, Гц.")]
    public float BobFrequency = 0.8f;

    [Tooltip("Скорость пульсации цвета, Гц.")]
    public float PulseSpeed = 0.5f;

    [Tooltip("Сила пульсации цвета: 0 — цвет постоянный, 1 — полный переход к SecondaryColor.")]
    [Range(0f, 1f)]
    public float PulseAmount = 0f;

    /// <summary>Кадры анимации юнита заданного типа (или null).</summary>
    public Sprite[] GetUnitFrames(UnitTypeName type)
    {
        FactionUnitVisuals visuals = FindUnitVisuals(type);
        if (visuals == null) return null;

        if (visuals.Frames != null && visuals.Frames.Length > 0) return visuals.Frames;
        if (visuals.Icon != null) return new[] { visuals.Icon };

        return null;
    }

    /// <summary>Иконка/статичный спрайт юнита заданного типа (или null).</summary>
    public Sprite GetUnitIcon(UnitTypeName type)
    {
        FactionUnitVisuals visuals = FindUnitVisuals(type);
        if (visuals == null) return null;

        if (visuals.Icon != null) return visuals.Icon;
        if (visuals.Frames != null && visuals.Frames.Length > 0) return visuals.Frames[0];

        return null;
    }

    /// <summary>Спрайт здания заданного типа (или null).</summary>
    public Sprite GetBuildingSprite(BuildingTypeNames type)
    {
        if (BuildingVisuals == null) return null;

        for (int i = 0; i < BuildingVisuals.Length; i++)
        {
            FactionBuildingVisuals visuals = BuildingVisuals[i];
            if (visuals == null || visuals.Sprite == null) continue;
            if (visuals.Type == type) return visuals.Sprite;
        }

        return null;
    }

    private FactionUnitVisuals FindUnitVisuals(UnitTypeName type)
    {
        if (UnitVisuals == null) return null;

        for (int i = 0; i < UnitVisuals.Length; i++)
        {
            FactionUnitVisuals visuals = UnitVisuals[i];
            if (visuals == null) continue;
            if (visuals.Type == type) return visuals;
        }

        return null;
    }
}
