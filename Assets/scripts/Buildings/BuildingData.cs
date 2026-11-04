using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/Building Data")]
public class BuildingData : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;

    [Tooltip("Что стоит постройка. Пусто — здание бесплатное.")]
    public ResourcePair[] ConstructionCost;

    [Tooltip("Тип здания: по нему BuilderSystem выбирает префаб из BuildingAtlas.")]
    public BuildingTypeNames BuildingTypeName;

    [Tooltip("Дополнительные условия размещения. Пусто — достаточно обычной проверки опоры и препятствий.")]
    public PlacementRule[] PlacementRules;

    public int MaxHealth;
    public int Width = 1;
    public int Height = 1;

    [Header("Production Settings")]
    public ResourcePair[] InputResources;
    public ResourcePair[] OutputResources;

    /// <summary>
    /// Проверяет дополнительные правила размещения. Пустой или незаполненный
    /// список правил считается удовлетворённым: правило — это ограничение,
    /// а не обязательное условие постройки.
    /// </summary>
    /// <param name="position">Центр здания в мировых координатах.</param>
    public bool SatisfiesPlacementRules(Vector2 position)
    {
        if (PlacementRules == null || PlacementRules.Length == 0) return true;

        for (int i = 0; i < PlacementRules.Length; i++)
        {
            PlacementRule rule = PlacementRules[i];
            if (rule == null) continue;

            if (!rule.IsSatisfied(position)) return false;
        }

        return true;
    }
}
