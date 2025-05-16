using UnityEngine;
using System.Collections.Generic;

public class MapChunk : MonoBehaviour
{
    public bool needsUpdate = false;

    private MeshFilter _meshFilter;
    private MeshCollider _meshCollider;
    private int _chunkSize;
    private TileAtlas _atlas = Map.Instance.atlas;

    public void Init(int chunkSize, TileAtlas atlas, Material material)
    {
        Debug.Log(atlas);

        _chunkSize = chunkSize;
        _atlas = atlas;

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.material = material;

        _meshCollider = gameObject.AddComponent<MeshCollider>();
    }

    void LateUpdate()
    {
        if (needsUpdate)
        {
            RebuildMesh();
            needsUpdate = false;
        }
    }

    public void RebuildMesh()
    {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        int triangleIndex = 0;

        for (int x = 0; x < _chunkSize; x++)
        {
            for (int y = 0; y < _chunkSize; y++)
            {
                Vector2Int worldPos = GetWorldPosition(x, y);
                Tile tile = Map.Instance.GetTile(worldPos.x, worldPos.y);

                if (tile.tileData.type == TileType.Air) continue;
                AddTileMesh(x, y, tile.tileData.type, ref vertices, ref triangles, ref uv, ref triangleIndex);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;
        _meshCollider.sharedMesh = mesh;
    }

    private void AddTileMesh(int x, int y, TileType type, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uv, ref int triangleIndex)
    {
        // Меш генерация для одного тайла
        Vector3[] tileVertices = {
            new Vector3(x, y),
            new Vector3(x + 1, y),
            new Vector3(x, y + 1),
            new Vector3(x + 1, y + 1)
        };

        vertices.AddRange(tileVertices);

        triangles.Add(triangleIndex);
        triangles.Add(triangleIndex + 2);
        triangles.Add(triangleIndex + 1);
        triangles.Add(triangleIndex + 1);
        triangles.Add(triangleIndex + 2);
        triangles.Add(triangleIndex + 3);

        // UV из атласа
        Rect texCoords = _atlas.GetUV(type);

        uv.Add(new Vector2(texCoords.xMin, texCoords.yMin));
        uv.Add(new Vector2(texCoords.xMax, texCoords.yMin));
        uv.Add(new Vector2(texCoords.xMin, texCoords.yMax));
        uv.Add(new Vector2(texCoords.xMax, texCoords.yMax));

        triangleIndex += 4;
    }

    private Vector2Int GetWorldPosition(int localX, int localY)
    {
        Vector2 chunkPos = transform.position;
        return new Vector2Int(
            Mathf.FloorToInt(chunkPos.x) + localX,
            Mathf.FloorToInt(chunkPos.y) + localY
        );
    }
}