using UnityEngine;

/// <summary>
/// Данные для главного здания колонии.
/// </summary>
[CreateAssetMenu(menuName = "Buildings/Main Building")]
public class MainBuildingData : BaseBuildingData
{
    [Header("Пассивный доход")]
    public float IncomeInterval = 0f;                // Интервал дохода (0 = нет)
    public ResourceBundle IncomeResources;           // Ресурсы за интервал

    [Header("Хранилище")]
    public int InitialStorageCapacity = 0;            // Увеличение лимитов ресурсов при постройке (опционально)
}