using UnityEngine;
using System.Collections;

public class Building : MonoBehaviour
{
    [SerializeField] protected BuildingData _data;

    [Header("���������")]
    public BuildingState State = BuildingState.Operational;
    public int CurrentHealth { get; set; }

    [Header("���������� �������")]
    [SerializeField] private GameObject selectionEffect;
    [SerializeField] private GameObject constructionEffect;

    private ResourceManager _resourceManager;
    private float _productionTimer = 0f;
    private bool _isProducing = false;

    public BuildingData Data => _data;

    protected virtual void Start()
    {
        CurrentHealth = _data.MaxHealth;
        _resourceManager = ResourceManager.Instance;

        // ��������� ������������ ���� ���� �������� �������
        if (_data.OutputResources.Resources != null && _data.OutputResources.Resources.Count > 0)
        {
            StartProduction();
        }

        // ������ �������������
        if (constructionEffect != null)
        {
            GameObject effect = Instantiate(constructionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }

    public virtual void Initialize(BuildingData data)
    {
        _data = data;
        CurrentHealth = data.MaxHealth;
        State = BuildingState.Operational;

        // ��������� ������ ����������
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null && data != null)
        {
            collider.size = new Vector2(data.Width, data.Height);
        }
    }

    public virtual void Update()
    {
        if (_isProducing && _resourceManager != null)
        {
            _productionTimer += Time.deltaTime;

            // ���������, ���� �� ������� ������� ��� ������������
            if (HasInputResources())
            {
                if (_productionTimer >= 5f) // �������� ������������ 5 ������
                {
                    ProduceResources();
                    _productionTimer = 0f;
                }
            }
        }
    }

    private void StartProduction()
    {
        _isProducing = true;
        Debug.Log($"{_data.DisplayName} ����� ������������");
    }

    private bool HasInputResources()
    {
        // ���� ������� �������� ���, ������ ������������ �� ������� ��������
        if (_data.InputResources.Resources == null || _data.InputResources.Resources.Count == 0)
            return true;

        return _resourceManager.HasResources(_data.InputResources);
    }

    private void ProduceResources()
    {
        // ��������� ������� �������, ���� ��� ����
        if (_data.InputResources.Resources != null && _data.InputResources.Resources.Count > 0)
        {
            if (!_resourceManager.TrySpendResources(_data.InputResources))
            {
                Debug.Log($"{_data.DisplayName}: ������������ ������� ��������");
                return;
            }
        }

        // ��������� �������� �������, ���� ��� ����
        if (_data.OutputResources.Resources != null && _data.OutputResources.Resources.Count > 0)
        {
            _resourceManager.AddResources(_data.OutputResources);
            Debug.Log($"{_data.DisplayName} �������� �������");
        }
    }

    public virtual void TakeDamage(int damage)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        if (CurrentHealth <= 0)
        {
            DestroyBuilding();
        }
    }

    protected virtual void DestroyBuilding()
    {
        State = BuildingState.Destroyed;

        // ���������� ����� �������� ��� ����������
        ReturnResourcesOnDestroy();

        Destroy(gameObject);
    }

    private void ReturnResourcesOnDestroy()
    {
        if (_resourceManager != null &&
            _data.ConstructionCost.Resources != null &&
            _data.ConstructionCost.Resources.Count > 0)
        {
            // ���������� 50% ���������
            ResourceBundle returnCost = new ResourceBundle();
            foreach (var resource in _data.ConstructionCost.Resources)
            {
                returnCost.Resources.Add(new ResourceBundle.ResourcePair
                {
                    Type = resource.Type,
                    Amount = Mathf.RoundToInt(resource.Amount * 0.5f)
                });
            }

            _resourceManager.AddResources(returnCost);
        }
    }

    public void Select()
    {
        if (selectionEffect != null)
        {
            selectionEffect.SetActive(true);
        }
    }

    public void Deselect()
    {
        if (selectionEffect != null)
        {
            selectionEffect.SetActive(false);
        }
    }

    private void OnMouseDown()
    {
        // ��������� ������ ��� �����
        SelectionManager.Instance?.SelectBuilding(this);
    }

    // ������������ ���� ������ � ���������
    private void OnDrawGizmosSelected()
    {
        if (_data != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(_data.Width, _data.Height, 0.1f));
        }
    }
}