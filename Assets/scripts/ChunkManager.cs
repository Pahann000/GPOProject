using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, реализующий загрузку чанков вокруг какого-либо объекта.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    private static ChunkManager _instance;
    [SerializeField] private int LoadingRadius = 2;
    private HashSet<Vector2Int> _requiredChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>();
    private List<IChunkObserver> _observers = new List<IChunkObserver>();

    /// <summary>
    /// Singleton-объект.
    /// </summary>
    public static ChunkManager Instance
    {
        get { return _instance; }
        private set { _instance = value; }

    }

    void Awake()
    {
        Instance = this;
        transform.position = Vector3.zero;
        Debug.Log("ChunkManager создан и инициализирован");

        
    }


    private void FixedUpdate()
    {
        UpdateRequiredChunks();
    }

    private void UpdateRequiredChunks()
    {
        _requiredChunks.Clear();

        foreach (var observer in _observers)
        {
            Vector2Int ObserverPosition = new Vector2Int(observer.X, observer.Y);

            for (int x = -LoadingRadius; x <= LoadingRadius; x++)
            {
                for (int y = -LoadingRadius; y <= LoadingRadius; y++)
                {
                    int ChunkPosX = ObserverPosition.x + x * Map.Instance.chunkSize;
                    int ChunkPosy = ObserverPosition.y + y * Map.Instance.chunkSize;

                    Vector2Int newChunkPosition = Map.Instance.GetChunkPosition(ChunkPosX, ChunkPosy);
                    _requiredChunks.Add(newChunkPosition);
                }
            }
        }

        foreach (Vector2Int requiredChunk in _requiredChunks)
        {
            // Загружаем новые чанки
            if (!_loadedChunks.Contains(requiredChunk))
            {
                Map.Instance.GenerateChunk(requiredChunk.x, requiredChunk.y);
                _loadedChunks.Add(requiredChunk);
            }
        }

        // Удаляем старые
        var chunksToRemove = new List<Vector2Int>();
        foreach (Vector2Int loadedChunk in _loadedChunks)
        {
            if (!_requiredChunks.Contains(loadedChunk))
            {
                chunksToRemove.Add(loadedChunk);
            }
        }

        foreach (Vector2Int chunk in chunksToRemove)
        {
            _loadedChunks.Remove(chunk);
            Map.Instance.DestroyChunk(chunk.x, chunk.y);
        }
    }

    /// <summary>
    /// Подписывает объект на генерацию чанков.
    /// Вокруг такого объекта будет генерироваться мир.
    /// </summary>
    /// <param name="observer"> Объект, вокруг которого требуется генерировать мир. </param>
    public void RegisterObserver(IChunkObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
            Debug.Log($"Наблюдатель зарегистрирован. Всего наблюдателей: {_observers.Count}");
        }
        Debug.Log($"Наблюдатель не зарегистрирован. Всего наблюдателей: {_observers.Count}");
    }

    /// <summary>
    /// Отписывает объект от генерации чанков.
    /// </summary>
    /// <param name="observer"> Отписываемый объект. </param>
    public void UnregisterObserver(IChunkObserver observer)
    {
        _observers.Remove(observer);
    }
}