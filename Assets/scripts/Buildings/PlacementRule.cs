using UnityEngine;

/// <summary>
/// Базовый класс правила размещения здания. Абстрактный: конкретные правила
/// создаются как ассеты (CreateAssetMenu) и назначаются в
/// <see cref="BuildingData.PlacementRules"/>.
/// </summary>
/// <remarks>
/// Классы правил лежат в отдельных файлах: Unity требует, чтобы имя файла
/// ScriptableObject-класса совпадало с именем класса, иначе ассет правила
/// сохраняется без скрипта.
/// </remarks>
public abstract class PlacementRule : ScriptableObject
{
    /// <summary>
    /// Проверяет, удовлетворяет ли позиция правилу размещения.
    /// </summary>
    /// <param name="position">Центр здания в мировых координатах.</param>
    /// <returns>True, если позиция удовлетворяет правилу.</returns>
    public abstract bool IsSatisfied(Vector2 position);
}
