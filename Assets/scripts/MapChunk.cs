using UnityEngine;
using System.Collections.Generic;

public class MapChunk : MonoBehaviour
{
    public bool needsUpdate = false;

    private MeshFilter _meshFilter;
    private int _chunkSize;
    private BlockAtlas _atlas;
    private CompositeCollider2D _collider;
    private Dictionary<Vector2Int, Collider2D> _colliders = new();

    public void Init(int chunkSize, BlockAtlas atlas, Material material)
    {
        _chunkSize = chunkSize;
        _atlas = atlas;

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.material = material;
        _collider = gameObject.AddComponent<CompositeCollider2D>();
        _collider.generationType = CompositeCollider2D.GenerationType.Synchronous;

        gameObject.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
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
                Block tile = Map.Instance.GetBlockInfo(worldPos.x, worldPos.y);

                if (tile.tileData.type == BlockType.Air) 
                {
                    DestroyCollider(worldPos);
                    continue;
                }
                
                AddTileMesh(x, y, tile.tileData.type, ref vertices, ref triangles, ref uv, ref triangleIndex);
                GenerateCollider(worldPos);
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;
    }

    private void DestroyCollider(Vector2Int worldPos)
    {
        if (_colliders.ContainsKey(worldPos))
        {
            GameObject ColliderGameObject = _colliders[worldPos].gameObject;
            _colliders.Remove(worldPos);
            Destroy(ColliderGameObject);
        }
    }

    private void GenerateCollider(Vector2Int worldPos)
    {
        if (_colliders.ContainsKey(worldPos)) { return; }

        GameObject colliderObj = new GameObject("ChunkCollider");
        colliderObj.transform.parent = transform;
        colliderObj.transform.position = (Vector2)worldPos + new Vector2(0.5f, 0.5f);

        var collider = colliderObj.AddComponent<BoxCollider2D>();
        collider.size = Vector2.one;
        collider.compositeOperation = Collider2D.CompositeOperation.Merge;

        colliderObj.layer = LayerMask.NameToLayer("Terrain");

        _colliders.Add(worldPos, collider);
    }

    private void AddTileMesh(int x, int y, BlockType type, ref List<Vector3> vertices, ref List<int> triangles, ref List<Vector2> uv, ref int triangleIndex)
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
        if (localX < 0 || localY < 0)
        {
            Debug.Log($"{chunkPos}, {localX}, {localY}");
        }

        return new Vector2Int(
            Mathf.FloorToInt(chunkPos.x) + localX,
            Mathf.FloorToInt(chunkPos.y) + localY
        );
    }
}