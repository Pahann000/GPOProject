using UnityEngine;
using System.Collections.Generic;
using System; // ← Добавьте эту директиву для Action

public class ResourceManager : MonoBehaviour
{
    // Добавляем событие для уведомления об изменениях ресурсов
    public static event Action<ResourceType> OnResourceChanged;

    public static ResourceManager Instance { get; private set; }

    private ResourceSystem _system;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (GameKernel.Instance != null)
        {
            _system = GameKernel.Instance.GetSystem<ResourceSystem>();

            GameKernel.Instance.EventBus.Subscribe<ResourceChangedEvent>(OnBusEvent);
        }
        else
        {
            Debug.LogError("CRITICAL: GameKernel не найден! ResourceSystem не работает!");
        }
    }

    private void OnBusEvent(ResourceChangedEvent evt)
    {
        OnResourceChanged?.Invoke(evt.Type);
    }

    // --- Прокси-методы ---

    public bool HasResources(ResourceBundle cost) => _system != null && _system.HasResources(cost);

    public bool TrySpendResources(ResourceBundle cost) => _system != null && _system.TrySpendResources(cost);

    public void AddResources(ResourceBundle income) => _system?.AddResources(income);

    public void AddResource(ResourceType type, int amount) => _system?.AddResource(type, amount);

    public int GetResource(ResourceType type) => _system != null ? _system.GetResource(type) : 0;

    public int GetStorageLimit(ResourceType type) => _system != null ? _system.GetStorageLimit(type) : 0;

    public void IncreaseStorage(ResourceType type, int amount) => _system?.IncreaseStorage(type, amount);

    public void DecreaseStorage(ResourceType type, int amount) => _system?.DecreaseStorage(type, amount);
}