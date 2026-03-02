using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, реализующий загрузку чанков вокруг какого-либо объекта.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    private Map _map;

    private static ChunkManager _instance;
    [SerializeField] private int LoadingRadius = 2;
    private HashSet<Vector2Int> _requiredChunks = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>();
    private List<IChunkObserver> _observers = new List<IChunkObserver>();

    public void Initialize(Map map)
    {
        _map = map;
        Debug.Log("[ChunkManager] Инициализирована.");
    }

    private void FixedUpdate()
    {
        // Если карта не инициализирована или модуль выключен - ничего не делай, блин.
        if (_map == null) return;

        UpdateRequiredChunks();
    }

    private void UpdateRequiredChunks()
    {
        _requiredChunks.Clear();

        if (_observers.Count == 0)
        {
            Debug.LogWarning("[ChunkManager] Нет активных наблюдателей!");
            return;
        }

        foreach (var observer in _observers)
        {
            if (observer == null) continue;

            Vector2Int ObserverPosition = new Vector2Int(observer.X, observer.Y);

            for (int x = -LoadingRadius; x <= LoadingRadius; x++)
            {
                for (int y = -LoadingRadius; y <= LoadingRadius; y++)
                {
                    int ChunkPosX = ObserverPosition.x + x * _map.ChunkSize;
                    int ChunkPosy = ObserverPosition.y + y * _map.ChunkSize;

                    Vector2Int newChunkPosition = _map.GetChunkPosition(ChunkPosX, ChunkPosy);
                    _requiredChunks.Add(newChunkPosition);
                }
            }
        }

        foreach (Vector2Int requiredChunk in _requiredChunks)
        {
            // Загружаем новые чанки
            if (!_loadedChunks.Contains(requiredChunk))
            {
                _map.GenerateChunk(requiredChunk.x, requiredChunk.y);
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
            _map.DestroyChunk(chunk.x, chunk.y);
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
        }
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