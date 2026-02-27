using UnityEngine;
using System;

/// <summary>
/// Базовый класс для всех зданий. Реализует IDamageable.
/// </summary>
public abstract class Building : MonoBehaviour, IDamagable
{
    [SerializeField] protected BaseBuildingData _data;
    public BaseBuildingData Data => _data;

    [Header("Состояние")]
    public BuildingState State = BuildingState.Operational;
    public int CurrentHealth { get; set; }

    [Header("Визуальные эффекты")]
    [SerializeField] protected GameObject selectionEffect;
    [SerializeField] protected GameObject constructionEffect;

    // События для оповещения других систем
    public event Action<Building> OnBuilt;
    public event Action<Building> OnDestroyed;

    // Координаты для интерфейса IDamageable
    public int X => Mathf.RoundToInt(transform.position.x);
    public int Y => Mathf.RoundToInt(transform.position.y);

    protected ResourceManager _resourceManager;

    protected virtual void Awake()
    {
        _resourceManager = ResourceManager.Instance;
    }

    public void NotifyBuilt()
    {
        OnBuilt?.Invoke(this);
    }

    protected virtual void Start()
    {
        if (_data != null)
        {
            CurrentHealth = _data.MaxHealth;
        }

        // Воспроизводим эффект строительства
        if (constructionEffect != null)
        {
            GameObject effect = Instantiate(constructionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 3f);
        }

        // Вызываем событие постройки (будет вызвано системой строительства после размещения)
        OnBuilt?.Invoke(this);
    }

    /// <summary>
    /// Инициализация здания данными (вызывается при создании).
    /// </summary>
    public virtual void Initialize(BaseBuildingData data)
    {
        _data = data;
        CurrentHealth = data.MaxHealth;
        State = BuildingState.Operational;

        // Настраиваем коллайдер
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        collider.size = new Vector2(data.Width, data.Height);
    }

    /// <summary>
    /// Вызывается каждый кадр. В базовом классе пустой, наследники могут переопределить.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// Нанесение урона зданию.
    /// </summary>
    public virtual void TakeDamage(int amount, Player damager, UnitTypeName unitType)
    {
        if (State != BuildingState.Operational) return;

        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            DestroyBuilding();
        }
        // Здесь можно добавить визуальную обратную связь
    }

    /// <summary>
    /// Разрушение здания.
    /// </summary>
    protected virtual void DestroyBuilding()
    {
        State = BuildingState.Destroyed;
        OnDestroyed?.Invoke(this);
        ReturnResourcesOnDestroy();
        Destroy(gameObject);
    }

    /// <summary>
    /// Возврат части ресурсов при разрушении.
    /// </summary>
    protected virtual void ReturnResourcesOnDestroy()
    {
        if (_resourceManager != null && _data?.ConstructionCost != null)
        {
            // Возвращаем 50% стоимости
            ResourceBundle returnCost = new ResourceBundle();
            foreach (var pair in _data.ConstructionCost.Resources)
            {
                returnCost.Resources.Add(new ResourceBundle.ResourcePair
                {
                    Type = pair.Type,
                    Amount = Mathf.RoundToInt(pair.Amount * 0.5f)
                });
            }
            _resourceManager.AddResources(returnCost);
        }
    }

    /// <summary>
    /// Выделение здания.
    /// </summary>
    public virtual void Select()
    {
        if (selectionEffect != null)
            selectionEffect.SetActive(true);
    }

    /// <summary>
    /// Снятие выделения.
    /// </summary>
    public virtual void Deselect()
    {
        if (selectionEffect != null)
            selectionEffect.SetActive(false);
    }

    // Обработка клика мышью (можно переопределить в наследниках)
    protected virtual void OnMouseDown()
    {
        SelectionManager.Instance?.SelectBuilding(this);
    }
}