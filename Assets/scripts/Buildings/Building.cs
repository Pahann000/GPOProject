using Mirror;
using System;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class Building : NetworkBehaviour, IDamagable
{
    [SerializeField] protected BaseBuildingData _data;
    public BaseBuildingData Data => _data;

    [Header("Состояние")]
    public BuildingState State = BuildingState.Operational;
    public int CurrentHealth { get; set; }

    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject selectionEffect;
    [SerializeField] private GameObject constructionEffect;

    // События для оповещения других систем
    public event Action<Building> OnBuilt;
    public event Action<Building> OnDestroyed;

    // Координаты для интерфейса IDamagable
    public int X => Mathf.RoundToInt(transform.position.x);
    public int Y => Mathf.RoundToInt(transform.position.y);

    protected ResourceSystem _resourceSystem;

    protected virtual void Awake()
    {
        // При старте получаем систему ресурсов через Ядро
        if (GameKernel.Instance != null)
        {
            _resourceSystem = GameKernel.Instance.GetSystem<ResourceSystem>();
        }
    }

    public void NotifyBuilt()
    {
        OnBuilt?.Invoke(this);
    }

    protected virtual void Start()
    {
        if (_resourceSystem == null && GameKernel.Instance != null)
        {
            _resourceSystem = GameKernel.Instance.GetSystem<ResourceSystem>();
        }

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

        OnBuilt?.Invoke(this);
    }

    /// <summary>
    /// Инициализация здания данными (вызывает при создании)
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

    public virtual void Update() { }

    /// <summary>
    /// Нанесение урона зданию
    /// </summary>
    public virtual void TakeDamage(int amount, Player Damager, UnitTypeName unitType)
    {
        if (State != BuildingState.Operational) return;

        CurrentHealth -= amount;
        if (CurrentHealth <= 0)
        {
            DestroyBuilding();
        }
    }

    /// <summary>
    /// Разрушение здания
    /// </summary>
    protected virtual void DestroyBuilding()
    {
        State = BuildingState.Destroyed;
        OnDestroyed?.Invoke(this);
        ReturnResourcesOnDestroy();
        Destroy(gameObject);
    }

    private void ReturnResourcesOnDestroy()
    {
        if (_resourceSystem != null && _data?.ConstructionCost != null && _data.Owner != null)
        {
            if (_data.ConstructionCost.Resources == null) return;

            List<ResourcePair> returnCost = new List<ResourcePair>();
            foreach (var kvp in _data.ConstructionCost.Resources)
            {
                returnCost.Add(new ResourcePair(kvp.Key, Mathf.RoundToInt(kvp.Value * 0.5f)));
            }
            _resourceSystem.AddResources(_data.Owner, returnCost.ToArray());
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
}