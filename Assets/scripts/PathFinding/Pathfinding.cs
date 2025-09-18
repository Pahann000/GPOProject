using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    private class Node
    {
        public int X { get; }
        public int Y { get; }
        public int GCost { get; set; } // Расстояние от старта
        public int HCost { get; set; } // Эвристическое расстояние до цели
        public int FCost => GCost + HCost;
        public Node Parent { get; set; }
        public bool IsWalkable { get; }

        public Node(int x, int y, bool isWalkable)
        {
            X = x;
            Y = y;
            IsWalkable = isWalkable;
        }
    }

    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int target)
    {
        Node startNode = new Node(start.x, start.y, IsWalkable(start.x, start.y));
        Node targetNode = new Node(target.x, target.y, IsWalkable(target.x, target.y));

        if (!startNode.IsWalkable || !targetNode.IsWalkable)
            return null;

        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < currentNode.FCost ||
                    (openSet[i].FCost == currentNode.FCost && openSet[i].HCost < currentNode.HCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(new Vector2Int(currentNode.X, currentNode.Y));

            if (currentNode.X == targetNode.X && currentNode.Y == targetNode.Y)
                return RetracePath(startNode, currentNode);

            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                if (!neighbour.IsWalkable || closedSet.Contains(new Vector2Int(neighbour.X, neighbour.Y)))
                    continue;

                int newMovementCostToNeighbour = currentNode.GCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.GCost || !openSet.Contains(neighbour))
                {
                    neighbour.GCost = newMovementCostToNeighbour;
                    neighbour.HCost = GetDistance(neighbour, targetNode);
                    neighbour.Parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                }
            }
        }

        return null; // Путь не найден
    }

    private static List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        // Проверяем соседей по горизонтали и вертикали
        int[] dx = { 0, 1, 0, -1 };
        int[] dy = { 1, 0, -1, 0 };

        for (int i = 0; i < 4; i++)
        {
            int checkX = node.X + dx[i];
            int checkY = node.Y + dy[i];

            if (checkX >= 0 && checkY >= 0)
            {
                neighbours.Add(new Node(
                    checkX,
                    checkY,
                    IsWalkable(checkX, checkY)
                ));
            }
        }

        return neighbours;
    }

    private static int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.X - nodeB.X);
        int dstY = Mathf.Abs(nodeA.Y - nodeB.Y);
        return dstX + dstY; // Манхэттенское расстояние
    }

    private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(new Vector2Int(currentNode.X, currentNode.Y));
            currentNode = currentNode.Parent;
        }
        path.Reverse();
        return path;
    }

    private static bool IsWalkable(int x, int y)
    {
        Block block = Map.Instance.GetBlock(x, y);
        return block != null && block.tileData.type == BlockType.Air;
    }
}