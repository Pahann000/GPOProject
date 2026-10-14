using UnityEngine;
using Mirror;

/// <summary>
/// Базовое здание: здоровье, состояние, производство ресурсов и визуальные эффекты.
/// Производство и урон выполняются только на сервере, а здоровье и состояние
/// синхронизируются клиентам через SyncVar (раньше здоровье писалось и на клиентах).
/// </summary>
public class Building : NetworkBehaviour
{
    [SerializeField] protected BuildingData _data;

    [SyncVar]
    private BuildingState _state = BuildingState.Operational;

    [SyncVar]
    private int _currentHealth;

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject selectionEffect;
    [SerializeField] private GameObject constructionEffect;

    /// <summary>Длительность одного цикла производства в секундах.</summary>
    [SerializeField] private float productionCycleSeconds = 5f;

    /// <summary>Владелец здания. Назначается на сервере при постройке.</summary>
    public Player Owner;

    private IResourceProvider _resourceManager;
    private float _productionTimer;
    private bool _isProducing;

    public BuildingData Data => _data;

    /// <summary>Текущее состояние здания (синхронизируется сервером).</summary>
    public BuildingState State
    {
        get => _state;
        protected set => _state = value;
    }

    /// <summary>Текущее здоровье здания (синхронизируется сервером).</summary>
    public int CurrentHealth
    {
        get => _currentHealth;
        private set => _currentHealth = value;
    }

    /// <summary>
    /// Входные ресурсы цикла производства. Вынесено в свойство, чтобы наследники
    /// могли задать свой источник данных, не дублируя сам цикл производства.
    /// </summary>
    protected virtual ResourcePair[] ProductionInputs => _data != null ? _data.InputResources : null;

    /// <summary>Выходные ресурсы цикла производства.</summary>
    protected virtual ResourcePair[] ProductionOutputs => _data != null ? _data.OutputResources : null;

    /// <summary>
    /// Экономика для наследников (бонусы складов, пассивный доход).
    /// Может быть null, если система недоступна.
    /// </summary>
    protected ResourceSystem Economy { get; private set; }

    protected virtual void Start()
    {
        if (_data == null)
        {
            Debug.LogError($"[Building] На префабе {name} не назначен BuildingData — здание не будет работать.");
            return;
        }

        if (isServer)
        {
            CurrentHealth = _data.MaxHealth;

            if (GameKernel.Instance != null && GameKernel.Instance.TryGetSystem(out ResourceSystem resourceSystem))
            {
                _resourceManager = resourceSystem;
                Economy = resourceSystem;
            }
            else
            {
                Debug.LogWarning($"[Building] ResourceSystem недоступен: производство {_data.DisplayName} отключено.");
            }

            StartProduction();
        }

        // Эффект постройки — чисто визуальный, на выделенном сервере он не нужен.
        if (isClient && constructionEffect != null)
        {
            GameObject effect = Instantiate(constructionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }
    }

    public virtual void Initialize(BuildingData data)
    {
        if (data == null) return;

        _data = data;
        CurrentHealth = data.MaxHealth;
        State = BuildingState.Operational;

        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = new Vector2(data.Width, data.Height);
        }
    }

    public virtual void Update()
    {
        // Производство — серверная механика. Раньше этот метод работал и на клиентах,
        // а «производит ли здание» зависело от того, нашёлся ли в процессе ResourceSystem.
        if (!isServer || !_isProducing) return;

        _productionTimer += Time.deltaTime;
        if (_productionTimer < productionCycleSeconds) return;

        _productionTimer = 0f;

        if (HasInputResources())
        {
            ProduceResources();
        }
    }

    private void StartProduction()
    {
        ResourcePair[] outputs = ProductionOutputs;
        if (outputs == null || outputs.Length == 0) return;

        _isProducing = true;
        Debug.Log($"[Building] {_data.DisplayName}: запущено производство");
    }

    private bool HasInputResources()
    {
        ResourcePair[] inputs = ProductionInputs;

        // Если входных ресурсов нет, производство ничего не тратит.
        if (inputs == null || inputs.Length == 0) return true;
        if (_resourceManager == null || Owner == null) return false;

        return _resourceManager.HasResources(Owner, inputs);
    }

    /// <summary>
    /// Один цикл производства. Наследники могут переопределить метод,
    /// чтобы добавить визуальные эффекты, вызвав base.ProduceResources().
    /// </summary>
    /// <returns>True, если цикл действительно произвёл ресурсы.</returns>
    protected virtual bool ProduceResources()
    {
        ResourcePair[] inputs = ProductionInputs;
        ResourcePair[] outputs = ProductionOutputs;

        if (inputs != null && inputs.Length > 0)
        {
            if (_resourceManager == null || Owner == null) return false;

            if (!_resourceManager.TrySpendResources(Owner, inputs))
            {
                Debug.Log($"[Building] {_data.DisplayName}: недостаточно ресурсов для производства");
                return false;
            }
        }

        if (outputs == null || outputs.Length == 0) return false;

        _resourceManager?.AddResources(Owner, outputs);
        return true;
    }

    /// <summary>
    /// Наносит урон зданию. Выполняется только на сервере: клиент не может
    /// инициировать уничтожение и возврат ресурсов.
    /// </summary>
    public virtual void TakeDamage(int damage)
    {
        if (!isServer || damage <= 0) return;

        // Защита от повторного «уничтожения» в одном кадре.
        if (CurrentHealth <= 0) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        if (CurrentHealth == 0)
        {
            DestroyBuilding();
        }
    }

    protected virtual void DestroyBuilding()
    {
        if (!isServer) return;

        State = BuildingState.Destroyed;

        // Возвращаем часть стоимости только на сервере
        ReturnResourcesOnDestroy();

        // Сетевой объект уничтожаем через NetworkServer, а не GameObject.Destroy,
        // иначе остаются «висячие» спавны и предупреждения Mirror.
        NetworkServer.Destroy(gameObject);
    }

    private void ReturnResourcesOnDestroy()
    {
        if (_resourceManager == null || Owner == null) return;

        ResourcePair[] cost = _data != null ? _data.ConstructionCost : null;
        if (cost == null || cost.Length == 0) return;

        // Возвращаем 50% стоимости постройки
        ResourcePair[] returnCost = new ResourcePair[cost.Length];
        for (int i = 0; i < cost.Length; i++)
        {
            returnCost[i] = new ResourcePair(cost[i].Type, cost[i].Amount / 2);
        }

        _resourceManager.AddResources(Owner, returnCost);
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

    private void OnDrawGizmosSelected()
    {
        if (_data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(_data.Width, _data.Height, 0.1f));
    }
}
