using UnityEngine;

[CreateAssetMenu(fileName = "Breakdown", menuName = "Incidents/Breakdown")]
public class BreakdownIncident : GameIncident
{
    public override bool CanTrigger(GameKernel kernel)
    {
        return Object.FindFirstObjectByType<Building>() != null;
    }

    public override void Execute(GameKernel kernel)
    {
        // Находим вообще все здания на карте
        Building[] allBuildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);

        if (allBuildings.Length == 0) return;

        Building target = allBuildings[Random.Range(0, allBuildings.Length)];

        target.State = BuildingState.Damaged;

        // В ProductionBuilding.cs уже есть проверка
        // Так что сломанное здание автоматически перестанет производить ресурсы

        Debug.Log($"<color=orange>[АВАРИЯ]</color> Поломка в здании: {target.Data.DisplayName}!");
    }
}