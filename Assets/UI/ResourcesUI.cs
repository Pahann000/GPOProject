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

    public void Initialize()
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
    public void CreateResourceItem(string key)
    {
        // ������� ����� �������
        var newItem = new ResourceItem(_resourceTemplate, key);

        // ��������� � ���������
        _resourcesContainer.Add(newItem.Root);
        _resources.Add(key, newItem);

        newItem.SetText($"{key}: {_player.Resources[key]}");
    }

    public void UpdateData()
    {
        foreach (var item in _resources)
        {
            ChangeResourceItem(item.Key);
        }
    }

    public void ChangeResourceItem(string key)
    {
        var resource = _resources[key];

        resource.SetText($"{key}: {_player.Resources[key]}");
    }
}