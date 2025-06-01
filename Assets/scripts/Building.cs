using UnityEngine;

/// <summary>
/// Абстрактный базовый класс для всех зданий в игре.
/// </summary>
/// <remarks>
/// Определяет общие свойства и поведение всех типов зданий:
/// - Состояние и здоровье
/// - Стоимость строительства
/// - Базовые требования к размещению
/// - Общую логику повреждения и разрушения
/// </remarks>
public abstract class Building : MonoBehaviour
{
    [SerializeField] protected BuildingData _data;

    [Header("Construction Cost")]
    public ResourceBundle ConstructionCost;
    public BuildingData Data => _data;
    public int CurrentHealth { get; protected set; }
    public BuildingState State { get; protected set; } = BuildingState.Planned;

    protected virtual void Start()
    {
        CurrentHealth = _data.MaxHealth;
    }

    public virtual void TakeDamage(int damage)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        if (CurrentHealth <= 0) DestroyBuilding();
    }

    protected virtual void DestroyBuilding()
    {
        State = BuildingState.Destroyed;
        Destroy(gameObject);
    }

    public virtual void CompleteConstruction()
    {
        State = BuildingState.Operational;
        CurrentHealth = _data.MaxHealth;
    }

    public virtual void Initialize(BuildingData data)
    {
        // Базовая реализация
        _data = data;
        CurrentHealth = data.MaxHealth;
    } 

    public virtual void Update() { }


}