using System;
using System.Collections.Generic;

/// <summary>
/// Чистая реализация поиска пути A* по сетке (4 направления, манхэттенская эвристика).
/// Не зависит от UnityEngine, поэтому покрывается обычными модульными тестами;
/// обвязку с картой и миром держит <see cref="Pathfinding"/>.
/// </summary>
/// <remarks>
/// Класс статический и держит переиспользуемые буферы: рассчитан на вызовы
/// из одного потока (главный поток игры).
/// Раньше узлы сравнивались по ссылке (List.Contains для объектов без
/// Equals/GetHashCode всегда false), минимум искался линейным сканом, а закрытые
/// узлы не переоткрывались: клетки попадали в open-set дублями, поиск был
/// фактически квадратичным, а путь — неоптимальным.
/// </remarks>
public static class GridPathfinder
{
    /// <summary>Значение «путь ещё не найден» (половина int.MaxValue, чтобы не было переполнения).</summary>
    public const int Unreachable = int.MaxValue / 2;

    /// <summary>Бюджет раскрытых узлов: недостижимая цель не должна сканировать всю карту.</summary>
    public const int DefaultMaxExpandedNodes = 10000;

    /// <summary>
    /// Шаг маршрута. <see cref="IsJump"/> помечает переходы через яму: их нельзя
    /// проходить обычным движением, юнит должен прыгнуть.
    /// </summary>
    public readonly struct PathStep
    {
        public readonly int X;
        public readonly int Y;
        public readonly bool IsJump;

        public PathStep(int x, int y, bool isJump)
        {
            X = x;
            Y = y;
            IsJump = isJump;
        }
    }

    /// <summary>
    /// Правила перемещения юнита по решётке. Все стоимости целочисленные, чтобы
    /// A* сравнивал их точно; стоимости шага и прыжка задают предпочтение:
    /// маршрут по земле дешевле, чем прыжки через ямы.
    /// </summary>
    public readonly struct MoveRules
    {
        /// <summary>Максимальный подъём за один переход (высота прыжка юнита в блоках).</summary>
        public readonly int MaxStepUp;

        /// <summary>Максимальный спуск за один переход (падение вниз).</summary>
        public readonly int MaxDrop;

        /// <summary>Максимальная ширина ямы, перепрыгиваемой одним прыжком (0 — без прыжков).</summary>
        public readonly int MaxJumpGap;

        /// <summary>Стоимость ровного шага.</summary>
        public readonly int WalkCost;

        /// <summary>Надбавка за каждый блок подъёма.</summary>
        public readonly int ClimbSurcharge;

        /// <summary>Надбавка за каждый блок спуска.</summary>
        public readonly int DropSurcharge;

        /// <summary>Стоимость прыжка: заметно выше шага, поэтому земля предпочтительнее.</summary>
        public readonly int JumpCost;

        /// <summary>Надбавка за каждую лишнюю клетку ширины ямы.</summary>
        public readonly int JumpGapSurcharge;

        public MoveRules(int maxStepUp, int maxDrop, int maxJumpGap, int walkCost,
            int climbSurcharge, int dropSurcharge, int jumpCost, int jumpGapSurcharge)
        {
            MaxStepUp = maxStepUp < 0 ? 0 : maxStepUp;
            MaxDrop = maxDrop < 0 ? 0 : maxDrop;
            MaxJumpGap = maxJumpGap < 0 ? 0 : maxJumpGap;
            WalkCost = walkCost > 0 ? walkCost : 1;
            ClimbSurcharge = climbSurcharge < 0 ? 0 : climbSurcharge;
            DropSurcharge = dropSurcharge < 0 ? 0 : dropSurcharge;
            JumpCost = jumpCost < 0 ? 0 : jumpCost;
            JumpGapSurcharge = jumpGapSurcharge < 0 ? 0 : jumpGapSurcharge;
        }

        /// <summary>Стандартные правила: подъём на блок, спуск на два, ямы до двух клеток.</summary>
        public static MoveRules Default => new MoveRules(1, 2, 2, 10, 8, 3, 35, 10);

        /// <summary>
        /// Только ходьба (совместимость и тесты): строго 4 направления, подъём и спуск
        /// выполняются вертикальными переходами, прыжков нет.
        /// </summary>
        public static MoveRules WalkOnly => new MoveRules(0, 0, 0, 10, 8, 3, 0, 0);
    }

    /// <summary>
    /// Масштаб эвристики: она должна быть не больше минимальной стоимости
    /// продвижения на одну клетку по манхэттену, иначе A* перестаёт быть
    /// оптимальным (спуск на две клетки стоит 16, а сокращает оценку на три
    /// единицы манхэттена — значит на единицу должно приходиться не больше 5).
    /// </summary>
    private const int HeuristicScale = 5;

    /// <summary>Узлы кэшируются по клетке и переиспользуются между запросами.</summary>
    private static readonly Dictionary<(int x, int y), Node> NodeCache = new Dictionary<(int x, int y), Node>();
    private static readonly HashSet<(int x, int y)> Closed = new HashSet<(int x, int y)>();
    private static readonly NodeHeap Open = new NodeHeap();

    /// <summary>Узлы, изменённые в текущем поиске: только их нужно сбрасывать перед следующим.</summary>
    private static readonly List<Node> Touched = new List<Node>();
    private static readonly List<PathStep> PathBuffer = new List<PathStep>();

    private sealed class Node
    {
        public int X;
        public int Y;
        public int GCost = Unreachable;
        public int HCost;
        public int FCost => GCost + HCost;
        public Node Parent;
        public bool Jump;
        public bool Touched;

        public void Reset()
        {
            GCost = Unreachable;
            HCost = 0;
            Parent = null;
            Jump = false;
        }
    }

    /// <summary>Запись кучи: F фиксируется на момент добавления, что позволяет отбрасывать устаревшие записи.</summary>
    private struct OpenEntry
    {
        public Node Node;
        public int F;
    }

    private sealed class NodeHeap
    {
        private readonly List<OpenEntry> _items = new List<OpenEntry>();

        public int Count => _items.Count;

        public void Clear()
        {
            _items.Clear();
        }

        public void Push(Node node)
        {
            var entry = new OpenEntry { Node = node, F = node.FCost };
            _items.Add(entry);

            int index = _items.Count - 1;
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (Compare(_items[index], _items[parent]) >= 0) break;

                Swap(index, parent);
                index = parent;
            }
        }

        public OpenEntry Pop()
        {
            OpenEntry result = _items[0];
            int lastIndex = _items.Count - 1;
            _items[0] = _items[lastIndex];
            _items.RemoveAt(lastIndex);

            int index = 0;
            while (true)
            {
                int left = 2 * index + 1;
                int right = left + 1;
                int smallest = index;

                if (left < _items.Count && Compare(_items[left], _items[smallest]) < 0) smallest = left;
                if (right < _items.Count && Compare(_items[right], _items[smallest]) < 0) smallest = right;
                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }

            return result;
        }

        private static int Compare(OpenEntry a, OpenEntry b)
        {
            int byF = a.F.CompareTo(b.F);
            return byF != 0 ? byF : a.Node.HCost.CompareTo(b.Node.HCost);
        }

        private void Swap(int a, int b)
        {
            OpenEntry temp = _items[a];
            _items[a] = _items[b];
            _items[b] = temp;
        }
    }

    /// <summary>
    /// Собирает клетки, с которых юнит может дотянуться до цели, в порядке предпочтения:
    /// сначала ближайшие к самой цели (обычно соседние), затем более далёкие в пределах
    /// радиуса; при равном расстоянии — ближе к юниту, и в последнюю очередь «крыша» цели.
    /// </summary>
    /// <remarks>
    /// Раньше путь вёл НА саму цель: юнит упирался в блок и оставался в состоянии
    /// движения навсегда. Список кандидатов позволяет сначала подойти вплотную,
    /// а если соседние клетки недоступны — остановиться на достижимом расстоянии
    /// в пределах радиуса добычи.
    /// </remarks>
    /// <param name="startX">Начальная клетка юнита по X.</param>
    /// <param name="startY">Начальная клетка юнита по Y.</param>
    /// <param name="targetX">Клетка цели по X.</param>
    /// <param name="targetY">Клетка цели по Y.</param>
    /// <param name="reachRadius">Радиус действия (добычи/атаки).</param>
    /// <param name="isWalkable">Предикат проходимости клетки.</param>
    /// <param name="results">Список, который заполняется кандидатами (очищается внутри).</param>
    public static void CollectGoalCandidates(int startX, int startY, int targetX, int targetY, int reachRadius,
        Func<int, int, bool> isWalkable, List<(int x, int y)> results)
    {
        if (results == null) return;

        results.Clear();
        if (isWalkable == null) return;

        // Сама цель может быть проходимой (например, юнит противника на земле).
        if (isWalkable(targetX, targetY))
        {
            results.Add((targetX, targetY));
            return;
        }

        int radius = reachRadius > 0 ? reachRadius : 1;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                if (Math.Abs(dx) + Math.Abs(dy) > radius) continue;   // манхэттенский радиус

                int x = targetX + dx;
                int y = targetY + dy;
                if (!isWalkable(x, y)) continue;

                results.Add((x, y));
            }
        }

        results.Sort((a, b) =>
        {
            int distanceToTargetA = Math.Abs(a.x - targetX) + Math.Abs(a.y - targetY);
            int distanceToTargetB = Math.Abs(b.x - targetX) + Math.Abs(b.y - targetY);
            if (distanceToTargetA != distanceToTargetB) return distanceToTargetA.CompareTo(distanceToTargetB);

            int distanceToStartA = Math.Abs(a.x - startX) + Math.Abs(a.y - startY);
            int distanceToStartB = Math.Abs(b.x - startX) + Math.Abs(b.y - startY);
            if (distanceToStartA != distanceToStartB) return distanceToStartA.CompareTo(distanceToStartB);

            int roofA = a.x == targetX && a.y > targetY ? 1 : 0;
            int roofB = b.x == targetX && b.y > targetY ? 1 : 0;
            return roofA.CompareTo(roofB);
        });
    }

    /// <summary>
    /// Ищет путь от старта до цели с учётом физики юнита: подъёмы, спуски и прыжки
    /// через ямы. Прыжки стоят дороже шага, поэтому из двух маршрутов выбирается
    /// тот, что идёт по земле, а яма перепрыгивается только если обход дороже.
    /// </summary>
    /// <param name="startX">Начальная клетка по X.</param>
    /// <param name="startY">Начальная клетка по Y.</param>
    /// <param name="goalX">Целевая клетка по X. Должна быть проходимой.</param>
    /// <param name="goalY">Целевая клетка по Y.</param>
    /// <param name="isWalkable">Предикат проходимости клетки.</param>
    /// <param name="rules">Правила перемещения юнита.</param>
    /// <param name="maxExpandedNodes">Бюджет раскрытых узлов.</param>
    /// <returns>Путь без стартовой клетки или null, если путь не найден.</returns>
    public static List<PathStep> FindPath(int startX, int startY, int goalX, int goalY,
        Func<int, int, bool> isWalkable, MoveRules rules, int maxExpandedNodes = DefaultMaxExpandedNodes)
    {
        if (isWalkable == null) return null;
        if (maxExpandedNodes <= 0) maxExpandedNodes = DefaultMaxExpandedNodes;

        // Цель обязана быть проходимой, иначе пути к ней не существует.
        if (!isWalkable(goalX, goalY)) return null;

        ResetBuffers();

        Node startNode = GetNode(startX, startY);
        startNode.GCost = 0;
        startNode.HCost = GetDistance(startX, startY, goalX, goalY) * HeuristicScale;
        PushNode(startNode);

        int expanded = 0;
        Node goalNode = null;

        while (Open.Count > 0 && expanded < maxExpandedNodes)
        {
            OpenEntry entry = Open.Pop();

            // Устаревшая запись: узел закрыт или его стоимость улучшилась
            // после того, как запись была добавлена.
            if (entry.F != entry.Node.FCost) continue;
            if (Closed.Contains((entry.Node.X, entry.Node.Y))) continue;

            Node current = entry.Node;
            Closed.Add((current.X, current.Y));
            expanded++;

            if (current.X == goalX && current.Y == goalY)
            {
                goalNode = current;
                break;
            }

            ExpandNeighbours(current, goalX, goalY, isWalkable, rules);
        }

        if (goalNode == null) return null;

        return BuildPath(startNode, goalNode);
    }

    /// <summary>
    /// Ищет путь обычной ходьбой, без прыжков (совместимый вариант).
    /// </summary>
    public static List<(int x, int y)> FindPath(int startX, int startY, int goalX, int goalY,
        Func<int, int, bool> isWalkable, int maxExpandedNodes = DefaultMaxExpandedNodes)
    {
        List<PathStep> steps = FindPath(startX, startY, goalX, goalY, isWalkable,
            MoveRules.WalkOnly, maxExpandedNodes);

        if (steps == null) return null;

        var result = new List<(int x, int y)>(steps.Count);
        for (int i = 0; i < steps.Count; i++)
        {
            result.Add((steps[i].X, steps[i].Y));
        }

        return result;
    }

    private static void ExpandNeighbours(Node current, int goalX, int goalY,
        Func<int, int, bool> isWalkable, MoveRules rules)
    {
        // Вертикальные переходы в своей колонке: подъём и спуск по уступу.
        TryAddMove(current, current.X, current.Y + 1, false, 0, goalX, goalY, isWalkable, rules);
        TryAddMove(current, current.X, current.Y - 1, false, 0, goalX, goalY, isWalkable, rules);

        for (int dir = -1; dir <= 1; dir += 2)
        {
            // Шаг в соседнюю колонку на высоте в пределах подъёма/спуска.
            for (int dy = -rules.MaxDrop; dy <= rules.MaxStepUp; dy++)
            {
                TryAddMove(current, current.X + dir, current.Y + dy, false, dy, goalX, goalY, isWalkable, rules);
            }

            // Прыжок через яму: сначала проверяем, что между клетками действительно
            // провал, иначе это обычный шаг и «прыгать» не нужно.
            for (int gap = 1; gap <= rules.MaxJumpGap; gap++)
            {
                if (!IsPit(current.X, current.Y, dir, gap, isWalkable)) continue;

                for (int dy = -rules.MaxDrop; dy <= rules.MaxStepUp; dy++)
                {
                    TryAddMove(current, current.X + dir * (gap + 1), current.Y + dy, true, dy,
                        goalX, goalY, isWalkable, rules);
                }
            }
        }
    }

    /// <summary>Проверяет, что между текущей клеткой и точкой приземления провал.</summary>
    private static bool IsPit(int x, int y, int dir, int gap, Func<int, int, bool> isWalkable)
    {
        for (int k = 1; k <= gap; k++)
        {
            if (isWalkable(x + dir * k, y)) return false;
        }

        return true;
    }

    private static void TryAddMove(Node current, int nx, int ny, bool isJump, int dy,
        int goalX, int goalY, Func<int, int, bool> isWalkable, MoveRules rules)
    {
        if (Closed.Contains((nx, ny))) return;
        if (!isWalkable(nx, ny)) return;

        int cost = GetMoveCost(current.X, current.Y, nx, ny, isJump, dy, rules);
        int tentativeG = current.GCost + cost;

        Node neighbour = GetNode(nx, ny);

        // Уже известный путь короче — ничего не меняем.
        if (tentativeG >= neighbour.GCost) return;

        neighbour.GCost = tentativeG;
        neighbour.HCost = GetDistance(nx, ny, goalX, goalY) * HeuristicScale;
        neighbour.Parent = current;
        neighbour.Jump = isJump;

        // В куче может остаться устаревшая запись для этого узла:
        // её отбросит проверка entry.F != Node.FCost.
        PushNode(neighbour);
    }

    /// <summary>
    /// Стоимость перехода. Прыжок дороже шага, подъём дороже ровного хода,
    /// спуск чуть дороже ровного хода — так маршрут по земле выигрывает у прыжков.
    /// </summary>
    private static int GetMoveCost(int x, int y, int nx, int ny, bool isJump, int dy, MoveRules rules)
    {
        int verticalDelta = Math.Abs(ny - y);
        int horizontalDelta = Math.Abs(nx - x);

        int cost = rules.WalkCost;

        if (dy > 0) cost += dy * rules.ClimbSurcharge;
        else if (dy < 0) cost += -dy * rules.DropSurcharge;

        // Вертикальные переходы в своей колонке тоже стоят надбавку.
        if (horizontalDelta == 0 && verticalDelta > 0)
        {
            int direction = ny - y;
            cost = rules.WalkCost + Math.Abs(direction) * (direction > 0 ? rules.ClimbSurcharge : rules.DropSurcharge);
        }

        if (!isJump) return cost;

        int gap = horizontalDelta - 1;
        return cost + rules.JumpCost + Math.Max(0, gap - 1) * rules.JumpGapSurcharge;
    }

    private static List<PathStep> BuildPath(Node startNode, Node goalNode)
    {
        PathBuffer.Clear();

        Node current = goalNode;
        int guard = 0;

        while (current != null && current != startNode && guard++ <= DefaultMaxExpandedNodes)
        {
            PathBuffer.Add(new PathStep(current.X, current.Y, current.Jump));
            current = current.Parent;
        }

        // Цепочка родителей оборвалась — путь невалиден.
        if (current != startNode) return null;

        PathBuffer.Reverse();

        // Возвращаем копию: буфер общий, а путь хранится у каждого юнита свой.
        return new List<PathStep>(PathBuffer);
    }

    private static void PushNode(Node node)
    {
        if (!node.Touched)
        {
            node.Touched = true;
            Touched.Add(node);
        }

        Open.Push(node);
    }

    private static Node GetNode(int x, int y)
    {
        var key = (x, y);
        if (NodeCache.TryGetValue(key, out Node node)) return node;

        node = new Node { X = x, Y = y };
        NodeCache[key] = node;
        return node;
    }

    private static void ResetBuffers()
    {
        // Сбрасываем только узлы прошлого поиска, а не весь кэш:
        // кэш ограничен размером карты и переиспользуется между запросами.
        foreach (Node node in Touched)
        {
            node.Reset();
            node.Touched = false;
        }

        Touched.Clear();
        Closed.Clear();
        Open.Clear();
    }

    private static int GetDistance(int ax, int ay, int bx, int by)
    {
        return Math.Abs(ax - bx) + Math.Abs(ay - by);
    }
}
