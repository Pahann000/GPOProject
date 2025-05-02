using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ���������������� ������� ���������� ��������� �������
/// (���������� ����� ��������)
/// </summary>
public class ResourceManager : MonoBehaviour
{
    // ������� ������ ��������
    private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();

    // ������������ �����������
    private Dictionary<ResourceType, int> _storageLimits = new Dictionary<ResourceType, int>();

    // ������ �� ��������� (��������)
    public static ResourceManager Instance { get; private set; }

    private void Awake()
    {
        // ������������� ���������
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

    /// <summary> ������������� ��������� �������� </summary>
    private void InitializeResources()
    {
        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            _resources[type] = 0;
            _storageLimits[type] = 1000; // ��������� ��������
        }

        // ��������� �������
        _resources[ResourceType.Metal] = 500;
        _resources[ResourceType.Water] = 200;
    }

    /// <summary> �������� ������� </summary>
    public void AddResource(ResourceType type, int amount)
    {
        _resources[type] = Mathf.Min(_resources[type] + amount, _storageLimits[type]);
    }

    /// <summary> ���������� ������������ ������� </summary>
    /// <returns> True ���� �������� ���������� � ��� ���� ������� </returns>
    public bool TryUseResource(ResourceType type, int amount)
    {
        if (_resources[type] >= amount)
        {
            _resources[type] -= amount;
            return true;
        }
        return false;
    }

    /// <summary> ��������� ����������� ��������� </summary>
    public void IncreaseStorage(ResourceType type, int amount)
    {
        _storageLimits[type] += amount;
    }

    /// <summary> ��������� ����������� ��������� </summary>
    public void DecreaseStorage(ResourceType type, int amount)
    {
        _storageLimits[type] -= amount;
    }

    /// <summary> ��������� ������� �������� �� ��������� ������ </summary>
    /// <returns> True ���� ���� �������� ���������� </returns>
    public bool HasResources(ResourceCost cost)
    {
        return _resources[ResourceType.Metal] >= cost.Metal &&
               _resources[ResourceType.Minerals] >= cost.Minerals &&
               _resources[ResourceType.Water] >= cost.Water;
    }

    /// <summary> ��������� ������� �� ��������� ������ </summary>
    public void SpendResources(ResourceCost cost)
    {
        _resources[ResourceType.Metal] -= cost.Metal;
        _resources[ResourceType.Minerals] -= cost.Minerals;
        _resources[ResourceType.Water] -= cost.Water;
    }
}

