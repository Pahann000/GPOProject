using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, реализующий загрузку чанков вокруг какого-либо объекта.
/// </summary>
public class ChunkManager : MonoBehaviour
{
    private Map _map;

    [SerializeField] private int loadingRadius = 2;
    private readonly HashSet<Vector2Int> _requiredChunks = new HashSet<Vector2Int>();
    private readonly HashSet<Vector2Int> _loadedChunks = new HashSet<Vector2Int>();
    private readonly List<IChunkObserver> _observers = new List<IChunkObserver>();

    /// <summary>Переиспользуемый буфер: раньше список аллоцировался каждый физический тик.</summary>
    private readonly List<Vector2Int> _chunksToRemove = new List<Vector2Int>();

    private bool _warnedNoObservers;

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
            // Предупреждение пишем один раз по изменению состояния, а не 50 раз в секунду.
            if (!_warnedNoObservers)
            {
                Debug.LogWarning("[ChunkManager] Нет активных наблюдателей!");
                _warnedNoObservers = true;
            }
        }
        else
        {
            _warnedNoObservers = false;

            // Идём с конца: уничтоженные наблюдатели удаляются из списка сразу,
            // иначе они оставались в нём навсегда и ломали обращение к transform.
            for (int i = _observers.Count - 1; i >= 0; i--)
            {
                IChunkObserver observer = _observers[i];

                if (!IsAlive(observer))
                {
                    _observers.RemoveAt(i);
                    continue;
                }

                Vector2Int ObserverPosition = new Vector2Int(observer.X, observer.Y);

                for (int x = -loadingRadius; x <= loadingRadius; x++)
                {
                    for (int y = -loadingRadius; y <= loadingRadius; y++)
                    {
                        int ChunkPosX = ObserverPosition.x + x * _map.ChunkSize;
                        int ChunkPosy = ObserverPosition.y + y * _map.ChunkSize;

                        Vector2Int newChunkPosition = _map.GetChunkPosition(ChunkPosX, ChunkPosy);
                        _requiredChunks.Add(newChunkPosition);
                    }
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
        _chunksToRemove.Clear();
        foreach (Vector2Int loadedChunk in _loadedChunks)
        {
            if (!_requiredChunks.Contains(loadedChunk))
            {
                _chunksToRemove.Add(loadedChunk);
            }
        }

        for (int i = 0; i < _chunksToRemove.Count; i++)
        {
            _loadedChunks.Remove(_chunksToRemove[i]);
            _map.DestroyChunk(_chunksToRemove[i].x, _chunksToRemove[i].y);
        }
    }

    /// <summary>
    /// Проверяет, жив ли наблюдатель. IChunkObserver — обычный интерфейс, поэтому
    /// сравнение с null не учитывает «фальшивый null» уничтоженных Unity-объектов.
    /// </summary>
    private static bool IsAlive(IChunkObserver observer)
    {
        if (observer == null) return false;

        return !(observer is Object unityObject) || unityObject != null;
    }

    /// <summary>
    /// Подписывает объект на генерацию чанков.
    /// Вокруг такого объекта будет генерироваться мир.
    /// </summary>
    /// <param name="observer"> Объект, вокруг которого требуется генерировать мир. </param>
    public void RegisterObserver(IChunkObserver observer)
    {
        if (observer == null) return;

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
        if (observer == null) return;

        _observers.Remove(observer);
    }
}
