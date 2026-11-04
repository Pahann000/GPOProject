using UnityEngine;

/// <summary>
/// Правило «рядом с водой»: в радиусе поиска должен быть коллайдер воды.
/// </summary>
[CreateAssetMenu(menuName = "Buildings/Rules/Near Water")]
public class NearWaterRule : PlacementRule
{
    [Tooltip("Радиус поиска водных объектов")]
    [SerializeField] private float _searchRadius = 3f;

    [Tooltip("Слой, на котором находятся водные объекты. Пусто — слой Water.")]
    [SerializeField] private LayerMask _waterLayer;

    public override bool IsSatisfied(Vector2 position)
    {
        int mask = _waterLayer.value != 0 ? _waterLayer.value : LayerMask.GetMask("Water");
        if (mask == 0) return false;

        return Physics2D.OverlapCircle(position, _searchRadius, mask);
    }
}
