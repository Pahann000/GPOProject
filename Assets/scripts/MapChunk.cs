using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Класс, описывающий отдельный чанк на карте(<see cref="Map"/>)
/// </summary>
public class MapChunk : MonoBehaviour
{
    private MeshFilter _meshFilter;
    private int _chunkSize;
    private BlockAtlas _atlas;
    private Transform _collidersParent;

    /// <summary>
    /// указывает, нужно ли обновить чанк.
    /// </summary>
    public bool needsUpdate { get; set; } = false;

    void LateUpdate()
    {
        if (needsUpdate)
        {
            StartCoroutine(RebuildMesh());
            needsUpdate = false;
        }
    }

    /// <summary>
    /// Инициализирует объект чанка.
    /// </summary>
    /// <param name="chunkSize"> Размер чанка. </param>
    /// <param name="atlas"> атлас со всеми видами блоков. </param>
    /// <param name="material"> материал (текстура). </param>
    public void Init(int chunkSize, BlockAtlas atlas, Material material)
    {
        _chunkSize = chunkSize;
        _atlas = atlas;

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.material = material;

        // Создаем родительский объект для коллайдеров (как в первом примере)
        _collidersParent = new GameObject("Colliders").transform;
        _collidersParent.parent = transform;
        _collidersParent.localPosition = Vector3.zero;

    }

    // Дальше только сущий кошмар...

    private IEnumerator RebuildMesh()
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
                    continue;
                }
                
                AddTileMesh(x, y, tile.tileData.type, ref vertices, ref triangles, ref uv, ref triangleIndex);
            }
            yield return null;
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.RecalculateNormals();

        _meshFilter.mesh = mesh;

        RebuildColliders();

        yield return null;
    }

    private void RebuildColliders()
    {
        // Очищаем старые коллайдеры
        foreach (Transform child in _collidersParent)
        {
            Destroy(child.gameObject);
        }

        // Создаем карту блоков для чанка
        bool[,] solidMap = new bool[_chunkSize, _chunkSize];
        for (int x = 0; x < _chunkSize; x++)
        {
            for (int y = 0; y < _chunkSize; y++)
            {
                Vector2Int worldPos = GetWorldPosition(x, y);
                Block tile = Map.Instance.GetBlockInfo(worldPos.x, worldPos.y);
                solidMap[x, y] = (tile.tileData.type != BlockType.Air);
            }
        }

        // Сканируем построчно и создаем объединенные коллайдеры
        for (int y = 0; y < _chunkSize; y++)
        {
            GameObject rowCollider = new GameObject("RowCollider_" + y);
            rowCollider.transform.parent = _collidersParent;
            rowCollider.layer = LayerMask.NameToLayer("Terrain");
            rowCollider.transform.localPosition = Vector3.zero;

            int currentSegmentStart = -1;
            int currentSegmentWidth = 0;

            for (int x = 0; x < _chunkSize; x++)
            {
                // Если блок твердый
                if (solidMap[x, y])
                {
                    // Если сегмент еще не начат, начинаем новый
                    if (currentSegmentStart == -1)
                    {
                        currentSegmentStart = x;
                        currentSegmentWidth = 1;
                    }
                    else
                    {
                        // Продолжаем существующий сегмент
                        currentSegmentWidth++;
                    }
                }
                else
                {
                    // Если встретили воздух и у нас есть активный сегмент
                    if (currentSegmentStart != -1)
                    {
                        // Завершаем текущий сегмент
                        CreateSegmentCollider(rowCollider, currentSegmentStart, y, currentSegmentWidth);
                        currentSegmentStart = -1;
                        currentSegmentWidth = 0;
                    }
                }
            }

            // Завершаем сегмент в конце строки, если он есть
            if (currentSegmentStart != -1)
            {
                CreateSegmentCollider(rowCollider, currentSegmentStart, y, currentSegmentWidth);
            }
        }
    }

    private void CreateSegmentCollider(GameObject parent, int startX, int y, int width)
    {
        GameObject segmentObj = new GameObject($"Segment_{startX}_{y}");
        segmentObj.transform.parent = parent.transform;
        segmentObj.layer = parent.layer;

        BoxCollider2D collider = segmentObj.AddComponent<BoxCollider2D>();

        // Рассчитываем позицию и размер
        // центр сегмента: startX + width/2, y + 0.5f
        collider.offset = new Vector2(width * 0.5f, 0.5f);
        collider.size = new Vector2(width, 1f);

        // Позиционируем в начале сегмента
        segmentObj.transform.localPosition = new Vector3(startX, y, 0);
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