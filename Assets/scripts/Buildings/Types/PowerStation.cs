using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "PowerPlantData", menuName = "Buildings/PowerPlant Data")]
public class PowerPlant : ProductionBuilding
{
    //[Header("Power Plant Settings")]
    //[SerializeField] private int energyProduction = 10;

    protected override void Start()
    {
        base.Start();

        inputResources = new ResourcePair[] { 
            new ResourcePair(ResourceType.Minerals, 2), 
            new ResourcePair(ResourceType.Ice, 1) 
        };

        outputResources = new ResourcePair[] { new ResourcePair(ResourceType.Minerals, 5) };
    }

    protected override IEnumerator ProductionAnimation()
    {
        productionAnimator.SetTrigger("EnergySurge");
        yield return new WaitForSeconds(0.5f);
    }
}