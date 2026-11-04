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

    /// <summary>
    /// Фракция-владелец. Нужна клиентам, чтобы нарисовать здание текстурой своей
    /// фракции: у людей и марсиан разные спрайты зданий.
    /// </summary>
    [SyncVar(hook = nameof(OnFactionChanged))]
    private FactionType _faction = FactionType.None;

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

    /// <summary>Фракция здания (копия у клиентов).</summary>
    public FactionType Faction => _faction;

    /// <summary>Тип здания — из данных префаба, одинаковый на всех пирах.</summary>
    public BuildingTypeNames TypeName => _data != null ? _data.BuildingTypeName : BuildingTypeNames.House;

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

    /// <summary>Здоровье не полное: здание можно ремонтировать.</summary>
    public bool CanBeRepaired => _data != null && CurrentHealth < _data.MaxHealth;

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

        // Клиент может получить здание уже после того, как в Start пришла
        // фракция: применяем текстуру здесь же, чтобы не ждать хука.
        if (isClient) ApplyFactionVisuals();
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

    /// <summary>
    /// Назначает фракцию здания. Вызывается только сервером, причём до
    /// <see cref="NetworkServer.Spawn"/>: клиентам значение приходит в стартовом
    /// пакете. Проверка идёт по <see cref="NetworkServer.active"/>, а не по isServer:
    /// у ещё не заспавненного объекта isServer ещё false, и раньше присваивание
    /// молча игнорировалось (здание оставалось без фракции).
    /// </summary>
    public void SetFactionServer(FactionType faction)
    {
        if (!NetworkServer.active) return;

        _faction = faction;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        ApplyFactionVisuals();
    }

    private void OnFactionChanged(FactionType oldFaction, FactionType newFaction)
    {
        ApplyFactionVisuals();
    }

    /// <summary>
    /// Подставляет текстуру здания выбранной фракции.
    /// </summary>
    private void ApplyFactionVisuals()
    {
        if (_data == null) return;

        FactionAtlas atlas = FactionAtlas.Cached;
        if (atlas == null) return;

        FactionVisuals.ApplyBuilding(gameObject, atlas.Get(_faction), _data.BuildingTypeName);
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

        // Изученные технологии увеличивают выход производства владельца.
        _resourceManager?.AddResources(Owner, ScaleResources(outputs, ProductionMultiplier));
        return true;
    }

    /// <summary>
    /// Множитель производства с учётом изученных исследований владельца.
    /// На клиенте и без <see cref="ResearchSystem"/> равен 1.
    /// </summary>
    protected float ProductionMultiplier
    {
        get
        {
            if (!isServer || Owner == null) return 1f;

            var kernel = GameKernel.Instance;
            if (kernel == null || !kernel.TryGetSystem(out ResearchSystem research)) return 1f;

            return research.GetProductionMultiplier(Owner);
        }
    }

    /// <summary>
    /// Умножает количество ресурсов на множитель (минимум 1 для ненулевых значений).
    /// Чистая функция — покрыта тестами.
    /// </summary>
    public static ResourcePair[] ScaleResources(ResourcePair[] resources, float multiplier)
    {
        if (resources == null || resources.Length == 0) return resources;
        if (multiplier <= 0f) multiplier = 0f;

        var scaled = new ResourcePair[resources.Length];

        for (int i = 0; i < resources.Length; i++)
        {
            ResourcePair source = resources[i];
            int amount = Mathf.RoundToInt(source.Amount * multiplier);

            // Ненулевой ресурс не должен обнуляться округлением, но нулевой
            // множитель (производство отключено) обязан давать ноль.
            if (amount <= 0 && source.Amount > 0 && multiplier > 0f) amount = 1;

            scaled[i] = new ResourcePair(source.Type, amount);
        }

        return scaled;
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

    /// <summary>
    /// Стоимость ремонта: доля стоимости постройки пропорциональна недостающему
    /// здоровью, но не меньше одной единицы каждого ресурса.
    /// Чистая функция — покрыта тестами.
    /// </summary>
    /// <returns>Массив расходов или null, если ремонт не нужен/невозможен.</returns>
    public static ResourcePair[] ComputeRepairCost(ResourcePair[] constructionCost, int currentHealth, int maxHealth)
    {
        if (constructionCost == null || constructionCost.Length == 0) return null;
        if (maxHealth <= 0) return null;
        if (currentHealth >= maxHealth) return null;
        if (currentHealth < 0) currentHealth = 0;

        float missingFraction = (maxHealth - currentHealth) / (float)maxHealth;

        var cost = new ResourcePair[constructionCost.Length];
        for (int i = 0; i < constructionCost.Length; i++)
        {
            ResourcePair source = constructionCost[i];
            int amount = Mathf.CeilToInt(source.Amount * missingFraction);

            // Хоть один ресурс, но ремонт должен стоить: иначе починка бесплатна.
            if (amount <= 0 && source.Amount > 0) amount = 1;

            cost[i] = new ResourcePair(source.Type, amount);
        }

        return cost;
    }

    /// <summary>
    /// Просит сервер разобрать здание. Вызывается кнопкой «Снести» в панели выделения.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdDemolish(NetworkConnectionToClient sender = null)
    {
        if (!isServer) return;

        Player caller = ResolveCaller(sender);
        if (!IsOwnedBy(caller))
        {
            Debug.LogWarning($"[Building] {name}: снос отклонён — здание принадлежит другому игроку.");
            return;
        }

        Debug.Log($"[Building] {name}: снесено владельцем.");
        DestroyBuilding();
    }

    /// <summary>
    /// Просит сервер отремонтировать здание за ресурсы владельца.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdRepair(NetworkConnectionToClient sender = null)
    {
        if (!isServer) return;
        if (_data == null) return;

        Player caller = ResolveCaller(sender);
        if (!IsOwnedBy(caller))
        {
            Debug.LogWarning($"[Building] {name}: ремонт отклонён — здание принадлежит другому игроку.");
            return;
        }

        if (!CanBeRepaired) return;

        if (Economy == null)
        {
            Debug.LogWarning($"[Building] {name}: ремонт невозможен, ResourceSystem недоступен.");
            return;
        }

        ResourcePair[] cost = ComputeRepairCost(_data.ConstructionCost, CurrentHealth, _data.MaxHealth);
        if (cost != null && !Economy.TrySpendResources(Owner, cost))
        {
            Debug.Log($"[Building] {name}: ремонт отклонён — недостаточно ресурсов.");
            return;
        }

        CurrentHealth = _data.MaxHealth;
        Debug.Log($"[Building] {name}: отремонтировано владельцем.");
    }

    /// <summary>
    /// Определяет игрока, отправившего команду. sender == null — прямой вызов на сервере.
    /// </summary>
    private static Player ResolveCaller(NetworkConnectionToClient sender)
    {
        return sender != null && sender.identity != null
            ? sender.identity.GetComponent<Player>()
            : null;
    }

    /// <summary>
    /// True, если здание принадлежит игроку. Выделенный сервер не имеет
    /// локального игрока, поэтому для прямого вызова на сервере владелец
    /// проверяется отдельно (см. ServerAuthorityTests).
    /// </summary>
    public bool IsOwnedBy(Player player)
    {
        return Owner != null && player != null && Owner == player;
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
