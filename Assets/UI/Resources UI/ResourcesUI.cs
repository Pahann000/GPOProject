using UnityEngine.UIElements;
using UnityEngine;
using System.Collections.Generic;

public class UIController : MonoBehaviour
{
    [SerializeField] private VisualTreeAsset _resourceTemplate;
    [SerializeField] private Player _player;

    private Dictionary<string, ResourceItem> _resources = new();
    private UIDocument _mainUI;
    private VisualElement _resourcesContainer;

    private void Start()
    {
        // �������� ������ �� ���������
        _mainUI = this.gameObject.GetComponent<UIDocument>();
        _resourcesContainer = _mainUI.rootVisualElement.Q<VisualElement>("resources-container");

        Initialize();
    }

    private void Initialize()
    {
        // ������������� ������������ ��������
        foreach (var pair in _player.Resources)
        {
            CreateResourceItem(pair.Key);
        }

        // �������� �� ��������� �������
        _player.Resources.ItemCahnged += ChangeResourceItem;
    }

    //����� ��� �������� ����� ���������
    private void CreateResourceItem(string key)
    {
        // ������� ����� �������
        var newItem = new ResourceItem(_resourceTemplate, key);

        // ��������� � ���������
        _resourcesContainer.Add(newItem.Root);
        _resources.Add(key, newItem);

        newItem.SetText($"{key}: {_player.Resources[key]}");
    }

    private void ChangeResourceItem(string key)
    {
        if (_resources.ContainsKey(key))
        {
            var resource = _resources[key];
            resource.SetText($"{key}: {_player.Resources[key]}");
        }
        else
        {
            CreateResourceItem(key);
        }
    }
}