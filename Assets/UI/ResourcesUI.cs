using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ResourcesUI : MonoBehaviour
{
    public VisualTreeAsset _resourceItemTemplate;

    private UIDocument _uiDocument;
    private VisualElement _resourcesContainer;
    private Dictionary<string, VisualElement> _resourceElements = new();
    private Player _player;

    private void Awake()
    {
        Initialize();
        var root = _uiDocument.rootVisualElement;
        _resourcesContainer = root.Q<VisualElement>("resources-container");
    }

    public void Initialize()
    {
        _uiDocument = this.gameObject.GetComponent<UIDocument>();
        _player = this.gameObject.GetComponent<Player>();

        // Инициализация существующих ресурсов
        foreach (var pair in _player.resources)
        {
            CreateOrUpdateResourceElement(pair.Key);
        }

        // Подписка на изменения словаря
        _player.resources.ItemCahnged += HandleResourceChanged;
    }

    private void HandleResourceChanged(string resourceType)
    {
        if (_player.resources.TryGetValue(resourceType, out int value))
        {
            CreateOrUpdateResourceElement(resourceType);
        }
        else
        {
            RemoveResourceElement(resourceType);
        }
    }

    private void CreateOrUpdateResourceElement(string resourceType)
    {
        var value = _player.resources[resourceType];

        if (_resourceElements.TryGetValue(resourceType, out var element))
        {
            UpdateExistingElement(element, resourceType, value);
        }
        else
        {
            CreateNewElement(resourceType, value);
        }
    }

    private void CreateNewElement(string resourceType, int value)
    {
        var newElement = _resourceItemTemplate.CloneTree();
        newElement.AddToClassList("resource-item");

        Debug.Log(_resourceItemTemplate.CloneTree().Q<Label>());

        var icon = newElement.Q<VisualElement>("resource-icon");
        var label = newElement.Q<Label>("resource-text");

        //icon.style.backgroundImage = Resources.Load<Texture2D>($"Icons/{resourceType}");
        label.text = $"{value}";
        label.name = $"{resourceType}-label";

        _resourcesContainer.Add(newElement);
        _resourceElements.Add(resourceType, newElement);
    }

    private void UpdateExistingElement(VisualElement element, string resourceType, int newValue)
    {
        var label = element.Q<Label>($"{resourceType}-label");
        label.text = $"{newValue}";

        element.schedule.Execute(() => {
            element.AddToClassList("changed");
            element.schedule.Execute(() => element.RemoveFromClassList("changed")).ExecuteLater(300);
        }).ExecuteLater(50);
    }

    private void RemoveResourceElement(string resourceType)
    {
        if (_resourceElements.TryGetValue(resourceType, out var element))
        {
            _resourcesContainer.Remove(element);
            _resourceElements.Remove(resourceType);
        }
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.resources.ItemCahnged -= HandleResourceChanged;
        }
    }
}