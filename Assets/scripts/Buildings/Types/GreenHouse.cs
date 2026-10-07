using UnityEngine;
using System.Collections;

/// <summary>
/// Теплица: перерабатывает лёд в пищу (ResourceType.Root).
/// Раньше теплица производила Energy, а электростанция — те же Minerals,
/// которые тратила на входе; данные были перепутаны между классами.
/// </summary>
public class Greenhouse : ProductionBuilding
{
    protected override void Start()
    {
        // До base.Start(): от ресурсов зависит, запустится ли производство.
        if (!HasConfiguredResources)
        {
            SetProductionResources(
                new[] { new ResourcePair(ResourceType.Ice, 1) },
                new[] { new ResourcePair(ResourceType.Root, 3) });
        }

        base.Start();
    }

    protected override IEnumerator ProductionAnimation()
    {
        productionAnimator.SetTrigger("Harvest");
        yield return new WaitForSeconds(0.8f);
    }
}
