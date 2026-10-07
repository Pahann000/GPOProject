using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace GPOProject.Tests
{
    /// <summary>
    /// Проверки алгоритма поиска пути (GridPathfinder) внутри Unity.
    /// Правила проходимости повторяют Pathfinding.IsWalkable: клетка проходима,
    /// если она пустая и под ней есть опора на 1 или 2 клетки ниже.
    /// </summary>
    public class GridPathfinderTests
    {
        /// <summary>Сетка с твёрдым полом на y = 0.</summary>
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

        private static void AssertValidPath(List<(int x, int y)> path, int sx, int sy, int gx, int gy,
            Func<int, int, bool> walkable)
        {
            Assert.IsNotNull(path, "путь не найден");

            int cx = sx, cy = sy;
            foreach ((int x, int y) in path)
            {
                Assert.AreEqual(1, Math.Abs(x - cx) + Math.Abs(y - cy), "шаг должен быть на 4-соседа");
                Assert.IsTrue(walkable(x, y), $"клетка ({x},{y}) не проходима");
                cx = x;
                cy = y;
            }

            Assert.AreEqual(gx, cx, "путь не доходит до цели по X");
            Assert.AreEqual(gy, cy, "путь не доходит до цели по Y");
        }

        [Test]
        public void StraightCorridor_PathIsOptimal()
        {
            var walkable = MakeGrid(10);
            var path = GridPathfinder.FindPath(0, 1, 9, 1, walkable);

            AssertValidPath(path, 0, 1, 9, 1, walkable);
            Assert.AreEqual(9, path.Count, "длина пути должна равняться манхэттенскому расстоянию");
        }

        [Test]
        public void StepObstacle_PathGoesOverIt()
        {
            var walkable = MakeGrid(11, extraSolid: new HashSet<(int x, int y)> { (5, 1) });
            var path = GridPathfinder.FindPath(1, 1, 9, 1, walkable);

            AssertValidPath(path, 1, 1, 9, 1, walkable);
            Assert.IsTrue(path.Exists(p => p.x == 5 && p.y == 2), "путь должен пройти по верху ступеньки");
            Assert.AreEqual(10, path.Count, "обход ступеньки даёт +2 к длине");
        }

        [Test]
        public void ImpassableWall_ReturnsNull()
        {
            var wall = new HashSet<(int x, int y)>();
            for (int y = 1; y <= 8; y++) wall.Add((5, y));

            var path = GridPathfinder.FindPath(1, 1, 10, 1, MakeGrid(12, wall));

            Assert.IsNull(path, "сквозь непроходимую стену пути быть не должно");
        }

        [Test]
        public void StartEqualsGoal_ReturnsEmptyPath()
        {
            var walkable = MakeGrid(5);
            var path = GridPathfinder.FindPath(3, 1, 3, 1, walkable);

            Assert.IsNotNull(path, "уже находясь в цели, путь должен быть пустым, а не null");
            Assert.AreEqual(0, path.Count);
        }

        [Test]
        public void GoalInsideWall_ReturnsNull()
        {
            var walkable = MakeGrid(6, new HashSet<(int x, int y)> { (4, 1) });
            var path = GridPathfinder.FindPath(1, 1, 4, 1, walkable);

            Assert.IsNull(path);
        }

        [Test]
        public void NodeBudget_StopsSearch()
        {
            var walkable = MakeGrid(60);

            Assert.IsNull(GridPathfinder.FindPath(1, 1, 55, 1, walkable, maxExpandedNodes: 3),
                "при крошечном бюджете поиск должен завершиться без результата");

            var full = GridPathfinder.FindPath(1, 1, 55, 1, walkable);
            Assert.IsNotNull(full, "с обычным бюджетом путь находится");
            Assert.AreEqual(54, full.Count);
        }

        [Test]
        public void RepeatedCalls_DoNotShareBuffers()
        {
            var walkable = MakeGrid(10);

            var first = GridPathfinder.FindPath(0, 1, 9, 1, walkable);
            var second = GridPathfinder.FindPath(9, 1, 0, 1, walkable);

            Assert.AreNotSame(first, second, "пути не должны делить один буфер");
            AssertValidPath(first, 0, 1, 9, 1, walkable);
            AssertValidPath(second, 9, 1, 0, 1, walkable);
        }

        [Test]
        public void FloorGap_ColumnIsImpassable()
        {
            var walkable = MakeGrid(12, holes: new HashSet<(int x, int y)> { (5, 0) });
            var path = GridPathfinder.FindPath(1, 1, 10, 1, walkable);

            Assert.IsNull(path, "колонка без опоры непроходима целиком");
        }

        // --- Выбор клетки назначения: юнит идёт не «на блок», а в пределы радиуса добычи ---

        [Test]
        public void GoalCandidates_SolidTarget_NearestToTargetComesFirst()
        {
            // Пол, блок-цель на (5,1). Юнит слева.
            var walkable = MakeGrid(12, new HashSet<(int x, int y)> { (5, 1) });
            var candidates = new List<(int x, int y)>();

            GridPathfinder.CollectGoalCandidates(1, 1, 5, 1, 3, walkable, candidates);

            Assert.IsTrue(candidates.Count > 0, "клетки для подхода должны найтись");
            Assert.IsFalse(candidates.Contains((5, 1)), "сам блок клеткой назначения быть не может");
            Assert.AreEqual((4, 1), candidates[0], "первой идёт соседняя с блоком клетка");
            Assert.IsTrue(walkable(candidates[0].x, candidates[0].y), "выбранная клетка должна быть проходимой");
        }

        [Test]
        public void GoalCandidates_PreferSideOverRoof()
        {
            var walkable = MakeGrid(12, new HashSet<(int x, int y)> { (5, 1) });
            var candidates = new List<(int x, int y)>();

            // Старт над целью: «крыша» (5,2) и бок (4,1) равноудалены от цели.
            GridPathfinder.CollectGoalCandidates(4, 2, 5, 1, 3, walkable, candidates);

            Assert.IsTrue(candidates.Count > 0);
            Assert.AreEqual((4, 1), candidates[0], "при равном расстоянии предпочитается подход сбоку, а не на крышу блока");
        }

        [Test]
        public void GoalCandidates_WalkableTarget_IsTheOnlyCandidate()
        {
            var walkable = MakeGrid(12);
            var candidates = new List<(int x, int y)>();

            GridPathfinder.CollectGoalCandidates(1, 1, 5, 1, 3, walkable, candidates);

            Assert.AreEqual(1, candidates.Count, "если цель сама проходима (враг на земле) — идём прямо к ней");
            Assert.AreEqual((5, 1), candidates[0]);
        }

        [Test]
        public void GoalCandidates_NothingInRadius_IsEmpty()
        {
            // Цель окружена породой, радиус маленький.
            var wall = new HashSet<(int x, int y)>();
            for (int x = 3; x <= 7; x++)
            {
                for (int y = 1; y <= 3; y++) wall.Add((x, y));
            }

            var walkable = MakeGrid(12, wall);
            var candidates = new List<(int x, int y)>();

            GridPathfinder.CollectGoalCandidates(1, 1, 5, 2, 1, walkable, candidates);

            Assert.AreEqual(0, candidates.Count, "в пределах радиуса нет проходимых клеток — путь строить некуда");
        }

        [Test]
        public void FindPath_ToBlock_StopsNextToItNotOnIt()
        {
            // Пол, блок-цель на (5,1): путь должен закончиться рядом с блоком.
            var walkable = MakeGrid(12, new HashSet<(int x, int y)> { (5, 1) });
            var candidates = new List<(int x, int y)>();
            GridPathfinder.CollectGoalCandidates(1, 1, 5, 1, 3, walkable, candidates);

            Assert.IsTrue(candidates.Count > 0, "клетка для подхода должна найтись");
            (int x, int y) goal = candidates[0];

            var path = GridPathfinder.FindPath(1, 1, goal.x, goal.y, walkable);

            Assert.IsNotNull(path, "путь к выбранной клетке должен находиться");
            Assert.IsTrue(path.Count > 0);

            (int x, int y) last = path[path.Count - 1];
            Assert.AreEqual(goal, last, "путь должен заканчиваться в выбранной клетке");
            Assert.AreNotEqual((5, 1), last, "путь не должен вести на сам блок");
            Assert.IsTrue(Math.Abs(last.x - 5) + Math.Abs(last.y - 1) <= 3,
                "последняя клетка пути должна быть в пределах радиуса добычи");
        }

        // --- Прыжки через ямы и приоритет маршрута по земле ---

        /// <summary>Проверяет маршрут с прыжками: обычные шаги — на соседа или с подъёмом/спуском.</summary>
        private static void AssertValidSteps(List<GridPathfinder.PathStep> path, int sx, int sy,
            Func<int, int, bool> walkable, GridPathfinder.MoveRules rules)
        {
            Assert.IsNotNull(path, "путь не найден");

            int cx = sx, cy = sy;
            foreach (GridPathfinder.PathStep step in path)
            {
                int dx = Math.Abs(step.X - cx);
                int dy = step.Y - cy;

                if (step.IsJump)
                {
                    Assert.Greater(dx, 1, "прыжок должен переносить через клетку");
                    Assert.LessOrEqual(dx, rules.MaxJumpGap + 1, "прыжок не должен быть длиннее возможного");
                }
                else
                {
                    Assert.LessOrEqual(dx, 1, "обычный шаг — не дальше соседней колонки");
                    Assert.LessOrEqual(dy, rules.MaxStepUp, "подъём не должен превышать высоту прыжка");
                    Assert.GreaterOrEqual(dy, -rules.MaxDrop, "спуск не должен превышать допустимый");
                }

                Assert.IsTrue(walkable(step.X, step.Y), $"клетка ({step.X},{step.Y}) не проходима");
                cx = step.X;
                cy = step.Y;
            }
        }

        [Test]
        public void Jump_ClearsOneCellPit()
        {
            // Пол с провалом в одну клетку на x = 5.
            var walkable = MakeGrid(12, holes: new HashSet<(int x, int y)> { (5, 0) });

            var path = GridPathfinder.FindPath(1, 1, 10, 1, walkable, GridPathfinder.MoveRules.Default);

            Assert.IsNotNull(path, "яму шириной в одну клетку юнит должен перепрыгивать");
            Assert.IsTrue(path.Exists(s => s.IsJump), "в маршруте должен быть прыжок");
            AssertValidSteps(path, 1, 1, walkable, GridPathfinder.MoveRules.Default);
        }

        [Test]
        public void Jump_ClearsTwoCellPit()
        {
            var walkable = MakeGrid(14, holes: new HashSet<(int x, int y)> { (5, 0), (6, 0) });

            var path = GridPathfinder.FindPath(1, 1, 12, 1, walkable, GridPathfinder.MoveRules.Default);

            Assert.IsNotNull(path, "яму шириной в две клетки юнит тоже должен перепрыгивать");

            int jumpIndex = path.FindIndex(s => s.IsJump);
            Assert.GreaterOrEqual(jumpIndex, 1, "прыжку должен предшествовать обычный шаг");

            int takeOffX = path[jumpIndex - 1].X;
            Assert.AreEqual(3, Math.Abs(path[jumpIndex].X - takeOffX),
                "через две клетки провала прыжок переносит на третью");
            AssertValidSteps(path, 1, 1, walkable, GridPathfinder.MoveRules.Default);
        }

        [Test]
        public void WidePit_WithWalkOnlyRules_HasNoPath()
        {
            var walkable = MakeGrid(14, holes: new HashSet<(int x, int y)> { (5, 0), (6, 0) });

            var path = GridPathfinder.FindPath(1, 1, 12, 1, walkable, GridPathfinder.MoveRules.WalkOnly);

            Assert.IsNull(path, "без прыжков яму не перейти");
        }

        [Test]
        public void ExpensiveJump_PrefersGroundDetour()
        {
            // Провал на x = 5 перекрыт сверху: над ним есть проход по земле (мостик на y = 2).
            var walkable = MakeGrid(12,
                extraSolid: new HashSet<(int x, int y)> { (5, 1) },
                holes: new HashSet<(int x, int y)> { (5, 0) });

            // Прыжок дорогой — маршрут обязан пойти по мостику, а не прыгать.
            var expensiveJump = new GridPathfinder.MoveRules(1, 2, 2, 10, 8, 3, 500, 100);
            var path = GridPathfinder.FindPath(1, 1, 10, 1, walkable, expensiveJump);

            Assert.IsNotNull(path, "путь по мостику должен найтись");
            Assert.IsFalse(path.Exists(s => s.IsJump), "при дорогом прыжке маршрут идёт по земле");
            AssertValidSteps(path, 1, 1, walkable, GridPathfinder.MoveRules.Default);
        }

        [Test]
        public void CheapJump_PrefersJumpOverGroundDetour()
        {
            var walkable = MakeGrid(12,
                extraSolid: new HashSet<(int x, int y)> { (5, 1) },
                holes: new HashSet<(int x, int y)> { (5, 0) });

            // Прыжок дешёвый — тот же мир проходится прыжком.
            var cheapJump = new GridPathfinder.MoveRules(1, 2, 2, 10, 8, 3, 1, 0);
            var path = GridPathfinder.FindPath(1, 1, 10, 1, walkable, cheapJump);

            Assert.IsNotNull(path);
            Assert.IsTrue(path.Exists(s => s.IsJump), "дешёвый прыжок выгоднее обхода");
            AssertValidSteps(path, 1, 1, walkable, GridPathfinder.MoveRules.Default);
        }

        [Test]
        public void Climb_OneBlockIsPassable_HighWallIsNot()
        {
            // Уступ в один блок: пол выше на клетку.
            var oneStep = MakeGrid(12, extraSolid: new HashSet<(int x, int y)> { (5, 0), (6, 0), (7, 0), (8, 0) });
            var path = GridPathfinder.FindPath(1, 1, 9, 1, oneStep, GridPathfinder.MoveRules.Default);
            Assert.IsNotNull(path, "по ровному полу путь есть");
            AssertValidSteps(path, 1, 1, oneStep, GridPathfinder.MoveRules.Default);

            // Стена в три блока: подъёма на такую высоту у юнита нет.
            var wall = MakeGrid(12, extraSolid: new HashSet<(int x, int y)>
            {
                (5, 1), (5, 2), (5, 3),
                (6, 1), (6, 2), (6, 3),
                (7, 1), (7, 2), (7, 3)
            });
            var blocked = GridPathfinder.FindPath(1, 1, 9, 1, wall, GridPathfinder.MoveRules.Default);
            Assert.IsNull(blocked, "стену в три блока юнит не перелезает");
        }
    }
}
