/// <summary>
/// Шахта: добывает минералы из камня.
/// Значения ниже — временные значения по умолчанию; если ресурсы заданы
/// в префабе (поля <see cref="ProductionBuilding"/>), приоритет у них.
/// </summary>
public class Mine : ProductionBuilding
{
    protected override void Start()
    {
        // Значения нужно задать ДО base.Start(): базовый Start() по ним
        // решает, запускать ли производство вообще.
        if (!HasConfiguredResources)
        {
            SetProductionResources(
                new[] { new ResourcePair(ResourceType.Rock, 1) },
                new[] { new ResourcePair(ResourceType.Minerals, 3) });
        }

        base.Start();
    }
}
