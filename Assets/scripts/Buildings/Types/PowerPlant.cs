using UnityEngine;
using System.Collections;

/// <summary>
/// Электростанция: производит энергию из минералов и льда.
/// Раньше на входе и на выходе были одни и те же минералы (2 → 5),
/// то есть ресурс создавался из ничего.
/// </summary>
public class PowerPlant : ProductionBuilding
{
    protected override void Start()
    {
        // До base.Start(): от ресурсов зависит, запустится ли производство.
        if (!HasConfiguredResources)
        {
            SetProductionResources(
                new[]
                {
                    new ResourcePair(ResourceType.Minerals, 2),
                    new ResourcePair(ResourceType.Ice, 1)
                },
                new[] { new ResourcePair(ResourceType.Energy, 5) });
        }

        base.Start();
    }

    protected override IEnumerator ProductionAnimation()
    {
        productionAnimator.SetTrigger("EnergySurge");
        yield return new WaitForSeconds(0.5f);
    }
}
