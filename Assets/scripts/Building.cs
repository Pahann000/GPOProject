using UnityEngine;
using System.Collections;

public class Building : MonoBehaviour
{
    [SerializeField] protected BuildingData _data;

    [Header("Состояние")]
    public BuildingState State = BuildingState.Operational;
    public int CurrentHealth { get; set; }

    [Header("Визуальные эффекты")]
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

        // Запускаем производство если есть выходные ресурсы
        if (_data.OutputResources.Resources != null && _data.OutputResources.Resources.Count > 0)
        {
            StartProduction();
        }

        // Эффект строительства
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

        // Обновляем размер коллайдера
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

            // Проверяем, есть ли входные ресурсы для производства
            if (HasInputResources())
            {
                if (_productionTimer >= 5f) // Интервал производства 5 секунд
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
        Debug.Log($"{_data.DisplayName} начал производство");
    }

    private bool HasInputResources()
    {
        // Если входных ресурсов нет, значит производство не требует ресурсов
        if (_data.InputResources.Resources == null || _data.InputResources.Resources.Count == 0)
            return true;

        return _resourceManager.HasResources(_data.InputResources);
    }

    private void ProduceResources()
    {
        // Списываем входные ресурсы, если они есть
        if (_data.InputResources.Resources != null && _data.InputResources.Resources.Count > 0)
        {
            if (!_resourceManager.TrySpendResources(_data.InputResources))
            {
                Debug.Log($"{_data.DisplayName}: недостаточно входных ресурсов");
                return;
            }
        }

        // Добавляем выходные ресурсы, если они есть
        if (_data.OutputResources.Resources != null && _data.OutputResources.Resources.Count > 0)
        {
            _resourceManager.AddResources(_data.OutputResources);
            Debug.Log($"{_data.DisplayName} произвел ресурсы");
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

        // Возвращаем часть ресурсов при разрушении
        ReturnResourcesOnDestroy();

        Destroy(gameObject);
    }

    private void ReturnResourcesOnDestroy()
    {
        if (_resourceManager != null &&
            _data.ConstructionCost.Resources != null &&
            _data.ConstructionCost.Resources.Count > 0)
        {
            // Возвращаем 50% стоимости
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
        // Выделение здания при клике
        SelectionManager.Instance?.SelectBuilding(this);
    }

    // Визуализация зоны здания в редакторе
    private void OnDrawGizmosSelected()
    {
        if (_data != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(_data.Width, _data.Height, 0.1f));
        }
    }
}