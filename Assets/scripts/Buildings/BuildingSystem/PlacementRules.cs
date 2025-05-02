using UnityEngine;

/// <summary>
/// Абстрактный базовый класс для правил размещения зданий
/// </summary>
public abstract class PlacementRule : ScriptableObject
{
    /// <summary>
    /// Проверяет, удовлетворяет ли позиция правилу размещения
    /// </summary>
    /// <param name="position">Позиция для проверки</param>
    /// <returns>True, если позиция удовлетворяет правилу</returns>
    public abstract bool IsSatisfied(Vector2 position);
}

/// <summary>
/// Конкретная реализация правила размещения "Рядом с водой"
/// </summary>
[CreateAssetMenu(menuName = "Buildings/Rules/Near Water")]
public class NearWaterRule : PlacementRule
{
    [Tooltip("Радиус поиска водных объектов")]
    [SerializeField] private float _searchRadius = 3f;

    [Tooltip("Слой, на котором находятся водные объекты")]
    [SerializeField] private LayerMask _waterLayer;

    /// <summary>
    /// Проверяет, находится ли позиция рядом с водой
    /// </summary>
    public override bool IsSatisfied(Vector2 position)
    {
        return Physics2D.OverlapCircle(position, _searchRadius, _waterLayer);
    }
}

