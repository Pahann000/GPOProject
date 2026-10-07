using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Класс, описывающий отдельный чанк на карте (<see cref="Map"/>).
/// Меш переиспользуется между перестройками, а не создаётся заново,
/// иначе каждая перестройка оставляла утечку Mesh-ассетов.
/// </summary>
public class MapChunk : MonoBehaviour
{
    private Map _map;
    private MeshFilter _meshFilter;
    private Mesh _mesh;
    private int _chunkSize;
    private BlockAtlas _atlas;
    private Transform _collidersParent;

    /// <summary>Мировая позиция левого нижнего блока чанка (кэш вместо чтения transform каждый блок).</summary>
    private Vector2Int _origin;

    /// <summary>Типы блоков чанка: один проход используется и мешом, и коллайдерами.</summary>
    private BlockType[,] _typeMap;

    /// <summary>Меш перестраивается корутиной: второй одновременный запуск дублировал коллайдеры.</summary>
    private bool _isRebuilding;

    /// <summary>
    /// указывает, нужно ли обновить чанк.
    /// </summary>
    public bool needsUpdate { get; set; } = false;

    private void LateUpdate()
    {
        if (!needsUpdate) return;

        // Если перестройка уже идёт, отметка останется на следующий кадр.
        if (_isRebuilding) return;

        needsUpdate = false;
        StartCoroutine(Rebuild());
    }

    private void OnDestroy()
    {
        // Меш создан кодом, значит его нужно и освободить.
        if (_mesh != null)
        {
            Destroy(_mesh);
            _mesh = null;
        }
    }

    /// <summary>
    /// Инициализирует объект чанка.
    /// </summary>
    /// <param name="map"> Карта, которой принадлежит чанк. </param>
    /// <param name="chunkSize"> Размер чанка. </param>
    /// <param name="atlas"> атлас со всеми видами блоков. </param>
    /// <param name="material"> материал (текстура). </param>
    public void Init(Map map, int chunkSize, BlockAtlas atlas, Material material)
    {
        _map = map;
        _chunkSize = chunkSize;
        _atlas = atlas;
        _typeMap = new BlockType[chunkSize, chunkSize];

        Vector3 position = transform.position;
        _origin = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();

        // sharedMaterial: обращение к .material создавало по приватной копии
        // материала на каждый чанк (ломает батчинг и течёт память).
        renderer.sharedMaterial = material;

        _mesh = new Mesh { name = $"ChunkMesh_{name}" };
        _meshFilter.sharedMesh = _mesh;

        // Создаем родительский объект для коллайдеров (как в первом примере)
        _collidersParent = new GameObject("Colliders").transform;
        _collidersParent.SetParent(transform, false);
        _collidersParent.localPosition = Vector3.zero;
    }

    // Дальше только сущий кошмар...

    private IEnumerator Rebuild()
    {
        _isRebuilding = true;

        // Один проход по блокам: тип нужен и для меша, и для коллайдеров.
        // Раньше было два прохода, и каждый блок резолвился через сервис-локатор.
        for (int x = 0; x < _chunkSize; x++)
        {
            for (int y = 0; y < _chunkSize; y++)
            {
                _typeMap[x, y] = _map != null
                    ? _map.GetBlockType(_origin.x + x, _origin.y + y)
                    : BlockType.Air;
            }

            yield return null;
        }

        BuildMesh();
        RebuildColliders();

        _isRebuilding = false;
        yield return null;
    }

    private void BuildMesh()
    {
        int tileCapacity = _chunkSize * _chunkSize;

        // Списки под размер чанка: раньше ToArray() удваивал пиковую память.
        var vertices = new List<Vector3>(tileCapacity * 4);
        var triangles = new List<int>(tileCapacity * 6);
        var uv = new List<Vector2>(tileCapacity * 4);

        int triangleIndex = 0;

        for (int x = 0; x < _chunkSize; x++)
        {
            for (int y = 0; y < _chunkSize; y++)
            {
                BlockType type = _typeMap[x, y];
                if (type == BlockType.Air) continue;

                AddTileMesh(x, y, type, vertices, triangles, uv, ref triangleIndex);
            }
        }

        _mesh.Clear();

        if (vertices.Count == 0) return;

        _mesh.SetVertices(vertices);
        _mesh.SetTriangles(triangles, 0);
        _mesh.SetUVs(0, uv);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
    }

    private void RebuildColliders()
    {
        // Очищаем старые коллайдеры
        for (int i = _collidersParent.childCount - 1; i >= 0; i--)
        {
            Destroy(_collidersParent.GetChild(i).gameObject);
        }

        // Сканируем построчно и создаем объединенные коллайдеры
        for (int y = 0; y < _chunkSize; y++)
        {
            if (!RowHasSolidBlock(y)) continue;   // пустые строки раньше тоже создавали объект

            GameObject rowCollider = new GameObject("RowCollider_" + y);
            rowCollider.transform.SetParent(_collidersParent, false);

            int currentSegmentStart = -1;
            int currentSegmentWidth = 0;

            for (int x = 0; x < _chunkSize; x++)
            {
                // Если блок твердый
                if (_typeMap[x, y] != BlockType.Air)
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

    private bool RowHasSolidBlock(int y)
    {
        for (int x = 0; x < _chunkSize; x++)
        {
            if (_typeMap[x, y] != BlockType.Air) return true;
        }

        return false;
    }

    private void CreateSegmentCollider(GameObject parent, int startX, int y, int width)
    {
        GameObject segmentObj = new GameObject($"Segment_{startX}_{y}");
        segmentObj.transform.SetParent(parent.transform, false);

        BoxCollider2D collider = segmentObj.AddComponent<BoxCollider2D>();

        // Рассчитываем позицию и размер
        // центр сегмента: startX + width/2, y + 0.5f
        collider.offset = new Vector2(width * 0.5f, 0.5f);
        collider.size = new Vector2(width, 1f);

        // Позиционируем в начале сегмента
        segmentObj.transform.localPosition = new Vector3(startX, y, 0);
    }

    private void AddTileMesh(int x, int y, BlockType type, List<Vector3> vertices,
        List<int> triangles, List<Vector2> uv, ref int triangleIndex)
    {
        // Меш генерация для одного тайла (без промежуточного массива на каждый тайл)
        vertices.Add(new Vector3(x, y));
        vertices.Add(new Vector3(x + 1, y));
        vertices.Add(new Vector3(x, y + 1));
        vertices.Add(new Vector3(x + 1, y + 1));

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
}
