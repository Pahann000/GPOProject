using UnityEngine;

/// <summary>
/// Конфигурация баланса зданий (ScriptableObject).
/// </summary>
[CreateAssetMenu(fileName = "BuildingBalanceConfig", menuName = "Mars Colony Building/BuildingBalanceConfig")]
public class BuildingBalanceConfig : ScriptableObject
{
    [Header("Типы зданий")]
    public BuildingTypeConfig[] BuildingTypes;

    [System.Serializable]
    public class BuildingTypeConfig
    {
        [Tooltip("Имя типа здания")]
        public string BuildingTypeName;

        [Tooltip("Название отображения")]
        public string DisplayName;

        [Tooltip("Максимальное здоровье")]
        public int MaxHealth;

        [Tooltip("Ширина здания")]
        public float Width;

        [Tooltip("Высота здания")]
        public float Height;

        [Tooltip("Стоимость строительства")]
        public ResourcePair[] ConstructionCost;

        [Tooltip("Входные ресурсы для производства")]
        public ResourcePair[] InputResources;

        [Tooltip("Выходные ресурсы")]
        public ResourcePair[] OutputResources;

        [Tooltip("Время производственного цикла в секундах")]
        public float ProductionCycleSeconds;
    }
}
