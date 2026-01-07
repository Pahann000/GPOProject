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

    private void Start()
    {
        InitializeResourceDisplays();
        UpdateResourceDisplay();

        // Подписываемся на события изменения ресурсов
        ResourceManager.OnResourceChanged += OnResourceChanged;
    }

    private void OnDestroy()
    {
        // Отписываемся от событий
        ResourceManager.OnResourceChanged -= OnResourceChanged;
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
            else
            {
                Debug.LogError($"ResourceDisplayController не найден на префабе для ресурса {type}");
            }
        }
    }

    private void Update()
    {
        UpdateResourceDisplay();
    }

    private void UpdateResourceDisplay()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            UpdateResourceDisplay(type);
        }
    }

    private void UpdateResourceDisplay(ResourceType type)
    {
        if (resourceControllers.ContainsKey(type) && ResourceManager.Instance != null)
        {
            int amount = ResourceManager.Instance.GetResource(type);
            int limit = ResourceManager.Instance.GetStorageLimit(type);

            resourceControllers[type].UpdateDisplay(amount, limit);
        }
    }

    private void OnResourceChanged(ResourceType type)
    {
        // При изменении ресурса обновляем только его отображение
        UpdateResourceDisplay(type);
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

    /// <summary>
    /// Внешний метод для принудительного обновления отображения
    /// </summary>
    public void ForceRefresh()
    {
        UpdateResourceDisplay();
    }

    /// <summary>
    /// Показывает/скрывает отображение конкретного ресурса
    /// </summary>
    public void SetResourceVisibility(ResourceType type, bool isVisible)
    {
        if (resourceControllers.ContainsKey(type))
        {
            resourceControllers[type].SetVisible(isVisible);
        }
    }
}