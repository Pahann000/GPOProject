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

        // Используем новый метод Create
        inputResources = ResourceBundle.Create(
            (ResourceType.Minerals, 2),
            (ResourceType.Water, 1)
        );

        outputResources = ResourceBundle.Create(
            (ResourceType.Energy, 5)
        );
    }

    protected override IEnumerator ProductionAnimation()
    {
        productionAnimator.SetTrigger("EnergySurge");
        yield return new WaitForSeconds(0.5f);
    }
}