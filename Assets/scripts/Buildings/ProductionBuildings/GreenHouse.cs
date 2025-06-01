using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "GreenhouseData", menuName = "Buildings/Greenhouse Data")]
public class Greenhouse : ProductionBuilding
{
    [Header("Greenhouse Settings")]
    [SerializeField] private int waterConsumption = 3;
    [SerializeField] private int foodProduction = 5;

    protected override void Start()
    {
        base.Start();

        // ���������� ����� ����� Create
        inputResources = ResourceBundle.Create(
            (ResourceType.Water, 1)
        );

        outputResources = ResourceBundle.Create(
            (ResourceType.Food, 3)
        );
    }

    protected override IEnumerator ProductionAnimation()
    {
        productionAnimator.SetTrigger("Harvest");
        yield return new WaitForSeconds(0.8f);
    }
}