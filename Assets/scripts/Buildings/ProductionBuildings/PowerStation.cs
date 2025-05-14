using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PowerPlantData", menuName = "Buildings/PowerPlant Data")]
public class PowerPlant : ProductionBuilding
{
    [Header("Power Plant Settings")]
    [SerializeField] private int energyProduction = 10;

    protected override void Start()
    {
        base.Start();
        outputResources = new ResourceBundle(
            (ResourceType.Energy, energyProduction)
        );
    }

    protected override IEnumerator ProductionAnimation()
    {
        productionAnimator.SetTrigger("EnergySurge");
        yield return new WaitForSeconds(0.5f);
    }
}