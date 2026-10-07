using System;
using System.Collections.Generic;
using System.Diagnostics;

// Модульные проверки GridPathfinder без Unity: подключается тот же файл исходников,
// что лежит в проекте (Assets/scripts/PathFinding/GridPathfinder.cs).
//
// Правила проходимости повторяют игру (Pathfinding.IsWalkable):
//   клетка проходима, если она пустая и под ней есть опора на 1 или 2 клетки ниже.

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static void Check(bool condition, string name)
    {
        if (condition)
        {
            _passed++;
            Console.WriteLine($"  PASS  {name}");
        }
        else
        {
            _failed++;
            Console.WriteLine($"  FAIL  {name}");
        }
    }

    /// <summary>
    /// Сетка с твёрдым полом на y = 0. extraSolid — дополнительные твёрдые блоки,
    /// holes — дырки в полу (клетки пола, которых нет).
    /// </summary>
    private static Func<int, int, bool> MakeGrid(int width,
        HashSet<(int x, int y)> extraSolid = null,
        HashSet<(int x, int y)> holes = null)
    {
        var solid = new HashSet<(int x, int y)>();
        for (int x = 0; x < width; x++) solid.Add((x, 0));
        if (extraSolid != null) solid.UnionWith(extraSolid);
        if (holes != null) solid.ExceptWith(holes);

        return (x, y) =>
        {
            if (x < 0 || x >= width) return false;
            if (y < 1) return false;
            if (solid.Contains((x, y))) return false;

            return solid.Contains((x, y - 1)) || solid.Contains((x, y - 2));
        };
    }

    /// <summary>
    /// Проверяет маршрут по новым правилам перемещения: обычный шаг — на 4 соседа
    /// или по диагонали на один блок (подъём/спуск), прыжок — через провал.
    /// </summary>
    private static bool IsValidPath(List<GridPathfinder.PathStep> path, int sx, int sy, int gx, int gy,
        Func<int, int, bool> walkable, GridPathfinder.MoveRules rules)
    {
        if (path == null) return false;

        int cx = sx, cy = sy;
        foreach (GridPathfinder.PathStep step in path)
        {
            int dx = Math.Abs(step.X - cx);
            int dy = step.Y - cy;

            if (step.IsJump)
            {
                if (dx < 2 || dx > rules.MaxJumpGap + 1) return false;
            }
            else
            {
                if (dx > 1) return false;
                if (dy > rules.MaxStepUp || dy < -rules.MaxDrop) return false;
                if (dx == 0 && Math.Abs(dy) != 1) return false;
            }

            if (!walkable(step.X, step.Y)) return false;
            cx = step.X;
            cy = step.Y;
        }

        return cx == gx && cy == gy;
    }

    /// <summary>Совместимость: путь обычной ходьбы без прыжков.</summary>
    private static bool IsValidPath(List<(int x, int y)> path, int sx, int sy, int gx, int gy,
        Func<int, int, bool> walkable)
    {
        if (path == null) return false;

        int cx = sx, cy = sy;
        foreach ((int x, int y) in path)
        {
            if (Math.Abs(x - cx) + Math.Abs(y - cy) != 1) return false;   // только 4 соседа
            if (!walkable(x, y)) return false;
            cx = x;
            cy = y;
        }

        return cx == gx && cy == gy;
    }

    private static void Main()
    {
        Console.WriteLine("GridPathfinder checks");

        // 1. Прямой коридор по ровному полу.
        {
            var walkable = MakeGrid(10);
            var path = GridPathfinder.FindPath(0, 1, 9, 1, walkable);

            Console.WriteLine("1) прямой коридор");
            Check(path != null, "путь найден");
            Check(IsValidPath(path, 0, 1, 9, 1, walkable), "путь валиден и заканчивается в цели");
            Check(path != null && path.Count == 9, $"длина равна манхэттенскому расстоянию (получено {path?.Count})");
        }

        // 2. Ступенька высотой 1: путь обязан подняться на неё и спуститься.
        {
            var walkable = MakeGrid(11, extraSolid: new HashSet<(int x, int y)> { (5, 1) });
            var path = GridPathfinder.FindPath(1, 1, 9, 1, walkable, GridPathfinder.MoveRules.Default);

            Console.WriteLine("2) ступенька высотой 1");
            Check(path != null, "путь найден");
            Check(IsValidPath(path, 1, 1, 9, 1, walkable, GridPathfinder.MoveRules.Default), "путь валиден");
            Check(path != null && path.Exists(p => p.X == 5 && p.Y >= 2), "путь проходит по верху ступеньки");
            Check(path != null && path.Count <= 10, $"обход ступеньки не длиннее прежнего (получено {path?.Count})");
        }

        // 3. Стена, которую нельзя перепрыгнуть.
        {
            var wall = new HashSet<(int x, int y)>();
            for (int y = 1; y <= 8; y++) wall.Add((5, y));

            var walkable = MakeGrid(12, wall);
            var path = GridPathfinder.FindPath(1, 1, 10, 1, walkable);

            Console.WriteLine("3) непроходимая стена");
            Check(path == null, "возвращается null");
        }

        // 4. Старт совпадает с целью.
        {
            var walkable = MakeGrid(5);
            var path = GridPathfinder.FindPath(3, 1, 3, 1, walkable);

            Console.WriteLine("4) старт == цель");
            Check(path != null, "возвращается пустой путь, а не null");
            Check(path != null && path.Count == 0, "путь пуст");
        }

        // 5. Цель внутри стены.
        {
            var walkable = MakeGrid(6, new HashSet<(int x, int y)> { (4, 1) });
            var path = GridPathfinder.FindPath(1, 1, 4, 1, walkable);

            Console.WriteLine("5) цель внутри стены");
            Check(path == null, "возвращается null");
        }

        // 6. Бюджет раскрытых узлов.
        {
            var walkable = MakeGrid(60);
            var path = GridPathfinder.FindPath(1, 1, 55, 1, walkable, maxExpandedNodes: 3);

            Console.WriteLine("6) ограничение бюджета");
            Check(path == null, "при крошечном бюджете путь не найден (без зависания)");

            var full = GridPathfinder.FindPath(1, 1, 55, 1, walkable);
            Check(full != null && full.Count == 54, "с обычным бюджетом путь находится");
        }

        // 7. Состояние буферов между вызовами.
        {
            var walkable = MakeGrid(10);
            var first = GridPathfinder.FindPath(0, 1, 9, 1, walkable);
            var second = GridPathfinder.FindPath(9, 1, 0, 1, walkable);

            Console.WriteLine("7) повторные вызовы");
            Check(IsValidPath(second, 9, 1, 0, 1, walkable), "второй путь валиден");
            Check(!ReferenceEquals(first, second), "пути не делят один буфер");
            Check(IsValidPath(first, 0, 1, 9, 1, walkable), "первый путь остался валидным");
        }

        // 8. Крупная сетка: скорость и оптимальность.
        {
            const int size = 200;
            var walkable = MakeGrid(size);

            var sw = Stopwatch.StartNew();
            var path = GridPathfinder.FindPath(1, 1, size - 1, 1, walkable);
            sw.Stop();

            Console.WriteLine($"8) большая сетка {size}x{size}");
            Check(IsValidPath(path, 1, 1, size - 1, 1, walkable), "путь валиден");
            Check(path != null && path.Count == size - 2, "путь оптимален по длине");
            Check(sw.ElapsedMilliseconds < 1000, $"поиск быстрый ({sw.ElapsedMilliseconds} мс)");
        }

        // 9. Разрыв в полу: колонка без опоры непроходима целиком.
        {
            var walkable = MakeGrid(12, holes: new HashSet<(int x, int y)> { (5, 0) });
            var path = GridPathfinder.FindPath(1, 1, 10, 1, walkable);

            Console.WriteLine("9) разрыв в полу");
            Check(path == null, "путь не найден (колонка без опоры непроходима)");
        }

        // 10. Серия запросов подряд: алгоритм не накапливает состояние.
        {
            var walkable = MakeGrid(30);
            bool allValid = true;

            for (int i = 0; i < 20; i++)
            {
                var path = GridPathfinder.FindPath(1, 1, 28, 1, walkable);
                if (!IsValidPath(path, 1, 1, 28, 1, walkable) || path.Count != 27) allValid = false;
            }

            Console.WriteLine("10) 20 запросов подряд");
            Check(allValid, "все пути валидны и одинаковой оптимальной длины");
        }

        // 11. Прыжок через яму в одну клетку.
        {
            var walkable = MakeGrid(12, holes: new HashSet<(int x, int y)> { (5, 0) });
            var path = GridPathfinder.FindPath(1, 1, 10, 1, walkable, GridPathfinder.MoveRules.Default);

            Console.WriteLine("11) яма в одну клетку");
            Check(path != null, "путь через яму найден");
            Check(path != null && path.Exists(s => s.IsJump), "в маршруте есть прыжок");
            Check(IsValidPath(path, 1, 1, 10, 1, walkable, GridPathfinder.MoveRules.Default), "путь валиден");
            Check(GridPathfinder.FindPath(1, 1, 10, 1, walkable, GridPathfinder.MoveRules.WalkOnly) == null,
                "без прыжков та же яма непроходима");
        }

        // 12. Прыжок через яму в две клетки.
        {
            var walkable = MakeGrid(14, holes: new HashSet<(int x, int y)> { (5, 0), (6, 0) });
            var path = GridPathfinder.FindPath(1, 1, 12, 1, walkable, GridPathfinder.MoveRules.Default);

            Console.WriteLine("12) яма в две клетки");
            Check(path != null, "путь через широкую яму найден");
            Check(path != null && path.Exists(s => s.IsJump), "в маршруте есть прыжок");
            Check(IsValidPath(path, 1, 1, 12, 1, walkable, GridPathfinder.MoveRules.Default), "путь валиден");
        }

        // 13. Приоритет земли: при дорогом прыжке маршрут идёт по мостику.
        {
            var bridged = MakeGrid(12,
                extraSolid: new HashSet<(int x, int y)> { (5, 1) },
                holes: new HashSet<(int x, int y)> { (5, 0) });

            var expensive = new GridPathfinder.MoveRules(1, 2, 2, 10, 8, 3, 500, 100);
            var path = GridPathfinder.FindPath(1, 1, 10, 1, bridged, expensive);

            Console.WriteLine("13) приоритет маршрута по земле");
            Check(path != null, "путь найден");
            Check(path != null && !path.Exists(s => s.IsJump), "дорогой прыжок не используется — идём по земле");
            Check(IsValidPath(path, 1, 1, 10, 1, bridged, expensive), "путь валиден");
        }

        Console.WriteLine();
        Console.WriteLine($"Итог: {_passed} passed, {_failed} failed");
        Environment.Exit(_failed == 0 ? 0 : 1);
    }
}
