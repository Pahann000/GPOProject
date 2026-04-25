using UnityEngine;
using System.Collections;

public class ProductionBuilding : Building
{
    [Header("Production Settings")]
    [SerializeField] public ResourcePair[] inputResources;
    [SerializeField] public ResourcePair[] outputResources;
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
        if (GameKernel.Instance.GetSystem<ResourceSystem>().TrySpendResources(_data.Owner, inputResources))
        {
            GameKernel.Instance.GetSystem<ResourceSystem>().AddResources(_data.Owner, outputResources);
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
        MultiplyResourceBundle(efficiencyMultiplier);
    }

    protected virtual void MultiplyResourceBundle(float multiplier)
    {
        for (int i = 0; i < outputResources.Length; i++) 
        {
            outputResources[i].Amount *= Mathf.RoundToInt(multiplier);
        }
    }

    public override void Initialize(BuildingData data)
    {
        base.Initialize(data);
        inputResources = data.InputResources;
        outputResources = data.OutputResources;
    }
}