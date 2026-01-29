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

[CreateAssetMenu(menuName = "Buildings/Rules/On Buildable Surface")]
public class OnBuildableSurfaceRule : PlacementRule
{
    [Header("Настройки")]
    //[Tooltip("Минимальное количество точек на поверхности")]
    //[SerializeField] private int minSurfacePoints = 2;
    [Tooltip("Расстояние проверки под точкой")]
    [SerializeField] private float checkDistance = 2f;
    [Tooltip("Слой строительной поверхности")]
    [SerializeField] private LayerMask buildableLayer;

    public override bool IsSatisfied(Vector2 position)
    {
        // Для простоты проверяем центральную точку
        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, checkDistance, buildableLayer);

        if (hit.collider != null)
        {
            Block block = hit.collider.GetComponent<Block>();
            if (block != null && block.tileData.type != BlockType.Air)
            {
                // Проверяем, можно ли строить на этом блоке
                // Если у BlockType есть свойство CanBuildOn, используем его
                return !IsResourceBlock(block);
            }
        }

        return false;
    }

    private bool IsResourceBlock(Block block)
    {
        // Определяем, является ли блок ресурсом
        string blockName = block.tileData.type.ToString().ToLower();
        return blockName.Contains("gold") || blockName.Contains("mineral") ||
               blockName.Contains("ice") || blockName.Contains("root");
    }
}

