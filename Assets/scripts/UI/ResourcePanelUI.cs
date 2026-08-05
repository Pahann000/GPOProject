using UnityEngine;
using System.Collections.Generic;

public class ResourcePanelUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject resourceDisplayPrefab;
    [SerializeField] private Transform resourcesContainer;

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
        //UpdateAllDisplays();
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
        if (resourcesContainer == null) resourcesContainer = transform;
        if (resourceDisplayPrefab == null) return;

        foreach (Transform child in resourcesContainer)
            Destroy(child.gameObject);

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            GameObject resourceGO = Instantiate(resourceDisplayPrefab, resourcesContainer);
            ResourceDisplayController controller = resourceGO.GetComponent<ResourceDisplayController>();

            if (controller != null)
            {
                // Берём сгенерированные C#-спрайты
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
            controller.UpdateDisplay(evt.NewAmount, evt.Limit);
        }
    }

    //private void UpdateAllDisplays()
    //{
    //    if (_resourceSystem == null) return;

    //    foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
    //    {
    //        int amount = _resourceSystem.GetResource(type);
    //        int limit = _resourceSystem.GetStorageLimit(type);
    //        if (resourceControllers.TryGetValue(type, out var controller))
    //        {
    //            controller.UpdateDisplay(amount, limit);
    //        }
    //    }
    //}

    private Sprite GetIconForResourceType(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Ice: return UISpriteGenerator.IceIcon;
            case ResourceType.Rock: return UISpriteGenerator.RockIcon;
            case ResourceType.Minerals: return UISpriteGenerator.MineralIcon;
            case ResourceType.Root:
            case ResourceType.Energy: return UISpriteGenerator.EnergyIcon;
            default: return null;
        }
    }
}
