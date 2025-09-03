using UnityEngine;
using System.Collections.Generic;

public class GraphGenerator
{
    public List<Vector2Int> GraphNodes = new();

    public void GenerateGraphNodes(Vector2Int startPosition)
    {
        TileType currentTileType = Map.Instance.GetTile(startPosition.x, startPosition.y).tileData.type;
        TileType underTileType = Map.Instance.GetTile(startPosition.x, startPosition.y-1).tileData.type;
        TileType upperTileType = Map.Instance.GetTile(startPosition.x, startPosition.y+1).tileData.type;
        GameObject node = new GameObject("Node");
        Vector3 nodePosition = Vector3.zero;
        if (underTileType != TileType.Air && currentTileType == TileType.Air)
        {
            GraphNodes.Add(startPosition);

            nodePosition = new Vector3(startPosition.x, startPosition.y, 0);
            node.transform.position = nodePosition;

            GenerateGraphNodes(startPosition + Vector2Int.left);
            GenerateGraphNodes(startPosition + Vector2Int.right);
        }
        else if (currentTileType != TileType.Air && upperTileType == TileType.Air)
        {
            GraphNodes.Add(startPosition + Vector2Int.up);

            nodePosition = new Vector3(startPosition.x, startPosition.y + 1, 0);
            node.transform.position = nodePosition;

            GenerateGraphNodes(startPosition + Vector2Int.up);
        }
        else
        {
            node = null;
        }
    }
}
