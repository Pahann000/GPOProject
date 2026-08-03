using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Здание, производящее ресурсы или спавнящее юнитов.
/// </summary>
public class ProduceBuilding : Building
{
    public ProduceBuildingData ProduceData => _data as ProduceBuildingData;
    private float _productionTimer = 0f;
    private bool _isProducing = true;

    protected override void Start()
    {
        base.Start();
        if (ProduceData == null)
        {
            Debug.LogError($"ProduceBuilding {name} не содержит ProduceBuildingData!");
            enabled = false;
            return;
        }
        _productionTimer = ProduceData.ProductionInterval;
    }

    public override void Update()
    {
        base.Update();

        if (State != BuildingState.Operational || !_isProducing) return;

        _productionTimer += Time.deltaTime;
        if (_productionTimer >= ProduceData.ProductionInterval)
        {
            TryProduce();
            _productionTimer = 0f;
        }
    }

    protected virtual void TryProduce()
    {
        if (ProduceData == null || _resourceSystem == null || _data.Owner == null) return;

        // Переводим List из ResourceBundle в массивы ResourcePair
        ResourcePair[] inputPairs = ProduceData.InputResources?.Resources != null
            ? ConvertBundleToPairs(ProduceData.InputResources)
            : new ResourcePair[0];

        ResourcePair[] outputPairs = ProduceData.OutputResources?.Resources != null
            ? ConvertBundleToPairs(ProduceData.OutputResources)
            : new ResourcePair[0];

        // Списываем входные ресурсы
        if (inputPairs.Length > 0)
        {
            if (!_resourceSystem.TrySpendResources(_data.Owner, inputPairs))
            {
                Debug.Log($"{ProduceData.DisplayName}: не хватает ресурсов для производства");
                return;
            }
        }

        // Производство или спавн
        if (ProduceData.SpawnsUnits)
        {
            SpawnUnits();
        }
        else if (outputPairs.Length > 0)
        {
            _resourceSystem.AddResources(_data.Owner, outputPairs);
        }

        PlayProductionEffects();
    }

    private ResourcePair[] ConvertBundleToPairs(ResourceBundle bundle)
    {
        if (bundle.Resources == null) return new ResourcePair[0];
        List<ResourcePair> pairs = new List<ResourcePair>();
        foreach (var kvp in bundle.Resources)
        {
            pairs.Add(new ResourcePair(kvp.Key, kvp.Value));
        }
        return pairs.ToArray();
    }

    protected virtual void SpawnUnits()
    {
        if (ProduceData.UnitPrefab == null) return;

        for (int i = 0; i < ProduceData.UnitsPerCycle; i++)
        {
            Vector3 spawnPos = transform.position;
            if (ProduceData.SpawnPoint != null)
                spawnPos = ProduceData.SpawnPoint.position;
            else
                spawnPos += new Vector3(ProduceData.Width * 0.5f, 0);

            Instantiate(ProduceData.UnitPrefab, spawnPos, Quaternion.identity);
        }
    }

    protected virtual void PlayProductionEffects()
    {
        Debug.Log($"{ProduceData.DisplayName} завершил цикл производства.");
    }

    public void SetProduction(bool enabled)
    {
        _isProducing = enabled;
    }
}