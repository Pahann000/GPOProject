using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Централизованная система управления ресурсами колонии
/// (Реализация через синглтон)
/// </summary>
public class ResourceManager : MonoBehaviour
{
    // Текущие запасы ресурсов
    private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

    // Максимальные вместимости
    private Dictionary<ResourceType, int> _storageLimits = new Dictionary<ResourceType, int>();

    // Ссылка на экземпляр (синглтон)
    public static ResourceManager Instance { get; private set; }

    private void Awake()
    {
        // Инициализация синглтона
        if (Instance == null)
        {
            Instance = this;
            InitializeResources();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary> Инициализация стартовых значений </summary>
    private void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _resources[type] = 0;
            _storageLimits[type] = 1000; // Дефолтное значение
        }

        // Стартовые ресурсы
        _resources[ResourceType.Metal] = 500;
        _resources[ResourceType.Water] = 200;
    }

    /// <summary> Добавить ресурсы </summary>
    public void AddResource(ResourceType type, int amount)
    {
        _resources[type] = Mathf.Min(_resources[type] + amount, _storageLimits[type]);
    }

    /// <summary> Попытаться использовать ресурсы </summary>
    /// <returns> True если ресурсов достаточно и они были списаны </returns>
    public bool TryUseResource(ResourceType type, int amount)
    {
        if (_resources[type] >= amount)
        {
            _resources[type] -= amount;
            return true;
        }
        return false;
    }

    /// <summary> Увеличить вместимость хранилища </summary>
    public void IncreaseStorage(ResourceType type, int amount)
    {
        _storageLimits[type] += amount;
    }

    /// <summary> Уменьшить вместимость хранилища </summary>
    public void DecreaseStorage(ResourceType type, int amount)
    {
        _storageLimits[type] -= amount;
    }

    /// <summary> Проверить наличие ресурсов по структуре затрат </summary>
    /// <returns> True если всех ресурсов достаточно </returns>
    public bool HasResources(ResourceCost cost)
    {
        return _resources[ResourceType.Metal] >= cost.Metal &&
               _resources[ResourceType.Minerals] >= cost.Minerals &&
               _resources[ResourceType.Water] >= cost.Water;
    }

    /// <summary> Потратить ресурсы по структуре затрат </summary>
    public void SpendResources(ResourceCost cost)
    {
        _resources[ResourceType.Metal] -= cost.Metal;
        _resources[ResourceType.Minerals] -= cost.Minerals;
        _resources[ResourceType.Water] -= cost.Water;
    }
}

