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
    [Header("Base Building Settings")]
    /// <summary>
    /// Отображаемое название здания в интерфейсе.
    /// </summary>
    public string DisplayName;

    /// <summary>
    /// Иконка здания для интерфейса и меню строительства.
    /// </summary>
    public Sprite Sprite;

    /// <summary>
    /// Максимальное количество здоровья здания.
    /// </summary>
    public int MaxHealth;

    /// <summary>
    /// Текущее количество здоровья здания.
    /// </summary>
    public int CurrentHealth;

    [Header("Construction Cost")]
    /// <summary>
    /// Стоимость строительства в металле.
    /// </summary>
    public int MetalCost;

    /// <summary>
    /// Стоимость строительства в минералах.
    /// </summary>
    public int MineralCost;

    /// <summary>
    /// Стоимость строительства в воде.
    /// </summary>
    public int WaterCost;

    [Header("Requirements")]
    /// <summary>
    /// Требуется ли ровная поверхность для строительства.
    /// </summary>
    public bool RequiresFlatSurface = true;

    /// <summary>
    /// Требуется ли подключение к энергосети для работы.
    /// </summary>
    public bool RequiresPower;

    /// <summary>
    /// Текущее состояние здания.
    /// </summary>
    public BuildingState State { get; protected set; } = BuildingState.Planned;

    /// <summary>
    /// Наносит урон зданию.
    /// </summary>
    /// <param name="damage">Количество наносимого урона</param>
    /// <remarks>
    /// Уменьшает CurrentHealth на указанное значение.
    /// При достижении здоровья нуля или ниже вызывает DestroyBuilding().
    /// </remarks>
    public virtual void TakeDamage(int damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            DestroyBuilding();
        }
    }

    /// <summary>
    /// Уничтожает здание.
    /// </summary>
    /// <remarks>
    /// Изменяет состояние на Destroyed и удаляет игровой объект.
    /// Может быть переопределен в дочерних классах для добавления специфичной логики.
    /// </remarks>
    protected virtual void DestroyBuilding()
    {
        State = BuildingState.Destroyed;
        // Визуальные эффекты разрушения
        Destroy(gameObject);
    }

    /// <summary>
    /// Завершает строительство здания.
    /// </summary>
    /// <remarks>
    /// Переводит здание в состояние Operational и восстанавливает здоровье до максимума.
    /// Может быть переопределен в дочерних классах для добавления дополнительной логики.
    /// </remarks>
    public virtual void CompleteConstruction()
    {
        State = BuildingState.Operational;
        CurrentHealth = MaxHealth;
    }

    /// <summary>
    /// Обновление состояния здания каждый кадр.
    /// </summary>
    /// <remarks>
    /// Базовая реализация пустая. Предназначен для переопределения в дочерних классах.
    /// </remarks>
    public virtual void Update()
    {
    }
}