using UnityEngine;
using System.Collections;

/// <summary>
/// Здание с производством и визуальными эффектами.
/// Сам цикл производства живёт в <see cref="Building"/>.
/// </summary>
public class ProductionBuilding : Building
{
    [Header("Production Settings")]
    [SerializeField] protected ResourcePair[] inputResources;
    [SerializeField] protected ResourcePair[] outputResources;

    [Header("Visuals")]
    [SerializeField] protected ParticleSystem productionParticles;
    [SerializeField] protected Animator productionAnimator;

    /// <summary>Данные префаба имеют приоритет над данными BuildingData.</summary>
    protected override ResourcePair[] ProductionInputs =>
        inputResources != null && inputResources.Length > 0 ? inputResources : base.ProductionInputs;

    /// <inheritdoc cref="ProductionInputs"/>
    protected override ResourcePair[] ProductionOutputs =>
        outputResources != null && outputResources.Length > 0 ? outputResources : base.ProductionOutputs;

    /// <summary>True, если ресурсы производства заданы в префабе, а не унаследованы от BuildingData.</summary>
    protected bool HasConfiguredResources =>
        (inputResources != null && inputResources.Length > 0) ||
        (outputResources != null && outputResources.Length > 0);

    /// <summary>Фактические входные ресурсы производства (для UI).</summary>
    public ResourcePair[] InputResources => ProductionInputs;

    /// <summary>Фактические выходные ресурсы производства (для UI).</summary>
    public ResourcePair[] OutputResources => ProductionOutputs;

    /// <summary>
    /// Задает ресурсы производства, копируя значения. Копия обязательна: запись
    /// по ссылке меняла бы сам ассет BuildingData и все здания этого типа.
    /// </summary>
    protected void SetProductionResources(ResourcePair[] inputs, ResourcePair[] outputs)
    {
        inputResources = Clone(inputs);
        outputResources = Clone(outputs);
    }

    private static ResourcePair[] Clone(ResourcePair[] source)
    {
        if (source == null) return null;

        var copy = new ResourcePair[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            copy[i] = new ResourcePair(source[i].Type, source[i].Amount);
        }

        return copy;
    }

    protected override bool ProduceResources()
    {
        if (!base.ProduceResources()) return false;

        PlayProductionEffects();
        return true;
    }

    private void PlayProductionEffects()
    {
        if (productionParticles != null)
            productionParticles.Play();

        if (productionAnimator != null)
            StartCoroutine(ProductionAnimation());
    }

    protected virtual IEnumerator ProductionAnimation()
    {
        productionAnimator.SetBool("Producing", true);
        yield return new WaitForSeconds(1f);
        productionAnimator.SetBool("Producing", false);
    }

    public override void Initialize(BuildingData data)
    {
        base.Initialize(data);

        if (data == null) return;

        // Копируем массивы: иначе апгрейд производства мутировал бы ассет.
        inputResources = Clone(data.InputResources);
        outputResources = Clone(data.OutputResources);
    }
}
