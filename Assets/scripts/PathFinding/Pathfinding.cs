using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Поиск пути по игровой карте: обвязка вокруг <see cref="GridPathfinder"/>.
/// Здесь только доступ к миру, выбор клетки назначения и преобразование координат;
/// сам алгоритм вынесен в чистый C# и покрыт тестами.
/// </summary>
public static class Pathfinding
{
    /// <summary>Кандидаты клеток назначения: переиспользуется между запросами.</summary>
    private static readonly List<(int x, int y)> GoalCandidates = new List<(int x, int y)>(64);

    /// <summary>
    /// Находит путь от start до клетки, с которой юнит может дотянуться до цели.
    /// </summary>
    /// <param name="start"> Начальная позиция поиска пути. </param>
    /// <param name="target"> Цель, до которой требуется найти путь. </param>
    /// <param name="reachRadius"> Радиус добычи/атаки: с этого расстояния юнит уже действует. </param>
    /// <param name="rules"> Правила перемещения юнита (подъём, спуск, прыжки через ямы). </param>
    /// <returns>
    /// Шаги маршрута до позиции, откуда цель достижима; null — если пути нет.
    /// </returns>
    public static List<GridPathfinder.PathStep> FindPath(Vector2Int start, Vector2Int target,
        int reachRadius = 1, GridPathfinder.MoveRules? rules = null)
    {
        Map map = ResolveMap();
        if (map == null) return null;

        GridPathfinder.MoveRules moveRules = rules ?? GridPathfinder.MoveRules.Default;

        GridPathfinder.CollectGoalCandidates(start.x, start.y, target.x, target.y, reachRadius,
            (x, y) => IsWalkable(map, x, y), GoalCandidates);

        // Кандидаты идут по предпочтению: сначала ближайшая к цели клетка (обычно
        // соседняя). Если до неё пути нет, пробуем следующие в пределах радиуса.
        for (int i = 0; i < GoalCandidates.Count; i++)
        {
            (int x, int y) goal = GoalCandidates[i];

            List<GridPathfinder.PathStep> path = GridPathfinder.FindPath(
                start.x, start.y, goal.x, goal.y, (x, y) => IsWalkable(map, x, y), moveRules);

            if (path != null) return path;
        }

        return null;
    }

    /// <summary>
    /// Клетка проходима: сама она пустая, а под ней есть опора (на одной или двух
    /// клетках ниже — юнит умеет прыгать).
    /// </summary>
    private static bool IsWalkable(Map map, int x, int y)
    {
        if (map.GetBlockType(x, y) != BlockType.Air) return false;

        return map.GetBlockType(x, y - 1) != BlockType.Air ||
               map.GetBlockType(x, y - 2) != BlockType.Air;
    }

    private static Map ResolveMap()
    {
        var kernel = GameKernel.Instance;
        if (kernel == null) return null;

        if (!kernel.TryGetSystem(out WorldSystem worldSystem)) return null;

        return worldSystem.WorldMap;
    }
}
