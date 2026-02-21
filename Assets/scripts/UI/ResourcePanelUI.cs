using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class ResourcePanelUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject resourceDisplayPrefab;
    [SerializeField] private Transform resourcesContainer;

    [Header("Resource Icons")]
    [SerializeField] private Sprite metalIcon;
    [SerializeField] private Sprite mineralsIcon;
    [SerializeField] private Sprite IceIcon;
    [SerializeField] private Sprite foodIcon;
    [SerializeField] private Sprite energyIcon;
    

    private Dictionary<ResourceType, ResourceDisplayController> resourceControllers = new Dictionary<ResourceType, ResourceDisplayController>();

    private ResourceSystem _resourceSystem;

    private void Start()
    {
        if (GameKernel.Instance != null)
        {
            _resourceSystem = GameKernel.Instance.GetSystem<ResourceSystem>();

            GameKernel.Instance.EventBus.Subscribe<ResourceChangedEvent>(OnResourceChangedBus);
        }

        InitializeResourceDisplays();
        UpdateAllDisplays();
    }

    private void OnDestroy()
    {
        if (GameKernel.Instance != null)
        {
            GameKernel.Instance.EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChangedBus);
        }
    }

    private void InitializeResourceDisplays()
    {
        // Очистите контейнер, если нужно
        foreach (Transform child in resourcesContainer)
            Destroy(child.gameObject);

        // Создайте отображение для каждого типа ресурса
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            GameObject resourceGO = Instantiate(resourceDisplayPrefab, resourcesContainer);
            ResourceDisplayController controller = resourceGO.GetComponent<ResourceDisplayController>();

            if (controller != null)
            {
                Sprite icon = GetIconForResourceType(type);
                controller.Initialize(type, icon);
                resourceControllers[type] = controller;
            }
        }
    }

    private void OnResourceChangedBus(ResourceChangedEvent evt)
    {
        if (resourceControllers.TryGetValue(evt.Type, out var controller))
        {
            int limit = _resourceSystem != null ? _resourceSystem.GetStorageLimit(evt.Type) : 1000;
            controller.UpdateDisplay(evt.NewAmount, limit);

            // controller.PlayChangeAnimation(evt.Delta > 0);
        }
    }

    private void UpdateAllDisplays()
    {
        if (_resourceSystem == null) return;

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            int amount = _resourceSystem.GetResource(type);
            int limit = _resourceSystem.GetStorageLimit(type);
            if (resourceControllers.TryGetValue(type, out var controller))
            {
                controller.UpdateDisplay(amount, limit);
            }
        }
    }

    private Sprite GetIconForResourceType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Rock: return metalIcon;
            case ResourceType.Minerals: return mineralsIcon;
            case ResourceType.Ice: return IceIcon;
            case ResourceType.Root: return foodIcon;
            case ResourceType.Energy: return energyIcon;
            default: return null;
        }
    }
}