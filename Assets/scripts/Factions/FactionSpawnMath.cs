using UnityEngine;

/// <summary>
/// Чистая математика фракций: кому какая фракция достаётся, где стоит база
/// и какой участок мира нужно расчистить. Вынесено из
/// <see cref="FactionSystem"/>, чтобы покрыть правила тестами без сцены и сети.
/// </summary>
public static class FactionSpawnMath
{
    /// <summary>
    /// Выбирает фракцию для подключающегося игрока по количеству уже играющих.
    /// Первый игрок (0 людей, 0 марсиан) уходит к людям, второй — к марсианам,
    /// третий снова к людям и так далее. Если одна фракция опустела (игрок вышел),
    /// следующий игрок занимает именно её.
    /// </summary>
    public static FactionType SelectFaction(int humansCount, int martiansCount)
    {
        if (humansCount < 0) humansCount = 0;
        if (martiansCount < 0) martiansCount = 0;

        return humansCount <= martiansCount ? FactionType.Humans : FactionType.Martians;
    }

    /// <summary>
    /// Колонка, в которой стоит база фракции. Отступ от краёв гарантирует,
    /// что площадка под базу не выйдет за границу карты.
    /// </summary>
    public static int GetBaseColumn(FactionDefinition definition, int mapWidth)
    {
        if (mapWidth <= 1) return 0;

        float fraction = definition != null ? definition.BaseMapFraction : 0.5f;
        int halfWidth = definition != null ? Mathf.Max(1, definition.ClearHalfWidth) : 1;
        int margin = halfWidth + 2;

        int column = Mathf.RoundToInt(mapWidth * Mathf.Clamp01(fraction));

        int min = Mathf.Min(margin, mapWidth - 1);
        int max = Mathf.Max(min, mapWidth - 1 - margin);

        return Mathf.Clamp(column, min, max);
    }

    /// <summary>
    /// Уровень пола базы: для людей — сама поверхность, для марсиан — глубина под ней.
    /// </summary>
    public static int ComputeFloorY(int surfaceY, bool spawnsOnSurface, int undergroundDepth)
    {
        if (spawnsOnSurface) return surfaceY;

        int depth = Mathf.Max(1, undergroundDepth);
        return Mathf.Max(1, surfaceY - depth);
    }

    /// <summary>
    /// Уровень пола наземной базы с запасом до верхней границы карты.
    /// </summary>
    /// <remarks>
    /// Генератор мира заполняет верхние слои породой почти везде, поэтому
    /// «поверхность» часто совпадает с верхней границей карты. Строить и ходить
    /// там негде: сверху уже нет блоков. Поэтому пол опускается на
    /// <paramref name="headroomBlocks"/> блоков ниже верха — над базой остаётся
    /// открытое место (её территория и так расчищается до самого верха карты).
    /// </remarks>
    public static int ClampSurfaceFloorY(int mapHeight, int surfaceFaceY, int headroomBlocks)
    {
        int headroom = Mathf.Max(1, headroomBlocks);
        int maxFloor = Mathf.Max(1, mapHeight - 1 - headroom);

        return Mathf.Clamp(surfaceFaceY, 1, maxFloor);
    }

    /// <summary>
    /// Прямоугольник расчистки: от пола базы вверх на высоту площадки.
    /// </summary>
    public static RectInt GetClearRect(int centerX, int floorY, int halfWidth, int height)
    {
        int hw = Mathf.Max(1, halfWidth);
        int h = Mathf.Max(1, height);

        return new RectInt(centerX - hw, floorY, hw * 2 + 1, h);
    }

    /// <summary>
    /// Точка появления игрока и юнитов: центр колонки, полблока над полом
    /// (коллайдер юнита высотой 1).
    /// </summary>
    public static Vector2 GetSpawnPosition(int centerX, int floorY)
    {
        return new Vector2(centerX + 0.5f, floorY + 0.5f);
    }
}
