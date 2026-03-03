using UnityEngine;

[CreateAssetMenu(fileName = "Breakdown", menuName = "Incidents/Breakdown")]
public class BreakdownIncident : GameIncident
{
    //public override bool CanTrigger(GameKernel kernel)
    //{
    //    return Object.FindFirstObjectByType<Building>() != null;
    //}

    // TODO: Придумать реализацию пожара.

    public override void Execute(GameKernel kernel)
    {
        // TODO: В Building.cs нужно будет добавить, что при состоянии здания Damaged оно переставало работать.

        Debug.Log("Критический сбой в здании каком-то.");
    }
}