using UnityEngine;
using System.Collections;
public class Mine : ProductionBuilding
{
    [Header("Mining Settings")]
    public float miningEfficiency = 1f;
    private ResourceDeposit targetDeposit;

    public int ProductionAmount { get; private set; }

    protected override void Start()
    {
        base.Start();
        FindNearestDeposit();
        UpdateOutputResources();
    }

    private void FindNearestDeposit()
    {
        ResourceDeposit[] deposits = FindObjectsOfType<ResourceDeposit>();
        float minDistance = float.MaxValue;

        foreach (var deposit in deposits)
        {
            if (deposit.IsDepleted) continue;

            float distance = Vector3.Distance(transform.position, deposit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                targetDeposit = deposit;
            }
        }
    }

    private void UpdateOutputResources()
    {
        if (targetDeposit != null)
        {
            outputResources = MultiplyResourceBundle(
                new ResourceBundle((targetDeposit.ResourceType, ProductionAmount)),
                miningEfficiency
            );
        }
    }

    protected override void TryProduceResources()
    {
        if (targetDeposit == null || targetDeposit.IsDepleted)
        {
            FindNearestDeposit();
            UpdateOutputResources();
        }

        if (targetDeposit != null && !targetDeposit.IsDepleted)
        {
            base.TryProduceResources();
            targetDeposit.ExtractResource(outputResources.Resources[targetDeposit.ResourceType]);
        }
    }

    protected override void UpgradeProduction(float efficiencyMultiplier)
    {
        base.UpgradeProduction(efficiencyMultiplier);
        miningEfficiency *= efficiencyMultiplier;
        UpdateOutputResources();
    }
}