using UnityEngine;

/// <summary>
/// Правило «на строительной поверхности»: под центром здания должен быть
/// твёрдый блок, на котором разрешено строить.
/// </summary>
/// <remarks>
/// Раньше правило искало блок через <c>hit.collider.GetComponent&lt;Block&gt;()</c>,
/// но <see cref="Block"/> — это обычный C#-класс, а не MonoBehaviour, поэтому
/// компонент не находился никогда и правило всегда возвращало false.
/// Теперь тип блока берётся из <see cref="WorldSystem"/>; физический луч
/// остаётся резервным вариантом, если карта ещё не создана.
/// </remarks>
[CreateAssetMenu(menuName = "Buildings/Rules/On Buildable Surface")]
public class OnBuildableSurfaceRule : PlacementRule
{
    [Tooltip("Глубина проверки под точкой, в блоках")]
    [SerializeField] private float checkDistance = 2f;

    [Tooltip("Слой строительной поверхности. Пусто — проверка идёт по карте мира.")]
    [SerializeField] private LayerMask buildableLayer;

    public override bool IsSatisfied(Vector2 position)
    {
        var kernel = GameKernel.Instance;

        if (kernel != null && kernel.TryGetSystem(out WorldSystem world) && world.WorldMap != null)
        {
            // Проверяем опору ровно под зданием: один блок вниз от центра.
            int x = Mathf.RoundToInt(position.x);
            int y = Mathf.FloorToInt(position.y - 0.5f) - 1;

            return IsBuildableBlock(world.GetBlockTypeAt(x, y));
        }

        // Карты ещё нет (например, проверка до генерации мира) — резервный луч.
        int mask = buildableLayer.value != 0
            ? buildableLayer.value
            : LayerMask.GetMask("Default", "Ground", "Buildable");

        if (mask == 0) return false;

        RaycastHit2D hit = Physics2D.Raycast(position, Vector2.down, checkDistance, mask);
        return hit.collider != null;
    }

    /// <summary>
    /// На блоках-ресурсах строить нельзя: их сначала нужно добыть.
    /// </summary>
    private static bool IsBuildableBlock(BlockType type)
    {
        switch (type)
        {
            case BlockType.Air:
            case BlockType.Gold:
            case BlockType.Minerals:
            case BlockType.Ice:
            case BlockType.Root:
                return false;
            default:
                return true;
        }
    }
}
