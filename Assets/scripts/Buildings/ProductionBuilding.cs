using UnityEngine;
using System.Collections;

public class ProductionBuilding : Building
{
    [Header("Production Settings")]
    [SerializeField] public ResourceBundle inputResources;
    [SerializeField] public ResourceBundle outputResources;
    [SerializeField] private float productionInterval = 5f;

    [Header("Visuals")]
    [SerializeField] public ParticleSystem productionParticles;
    [SerializeField] public Animator productionAnimator;

    private float lastProductionTime;

    public override void Update()
    {
        base.Update();

        if (State != BuildingState.Operational) return;

        if (Time.time - lastProductionTime > productionInterval)
        {
            TryProduceResources();
        }
    }

    protected virtual void TryProduceResources()
    {
        if (ResourceManager.Instance.TrySpendResources(inputResources))
        {
            ResourceManager.Instance.AddResources(outputResources);
            PlayProductionEffects();
            lastProductionTime = Time.time;
        }
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

    protected virtual void UpgradeProduction(float efficiencyMultiplier)
    {
        productionInterval *= efficiencyMultiplier;
        outputResources = MultiplyResourceBundle(outputResources, efficiencyMultiplier);
    }

    protected virtual ResourceBundle MultiplyResourceBundle(ResourceBundle bundle, float multiplier)
    {
        var newResources = new ResourceBundle();
        foreach (var kvp in bundle.Resources)
        {
            newResources.Resources[kvp.Key] = Mathf.RoundToInt(kvp.Value * multiplier);
        }
        return newResources;
    }

    public override void Initialize(BuildingData data)
    {
        base.Initialize(data); // Вызываем базовую реализацию
        // Дополнительная инициализация
        inputResources = data.InputResources;
        outputResources = data.OutputResources;
    }
}