using UnityEngine;

/// <summary>
/// «дание, которое производит ресурсы или юнитов с определЄнным интервалом.
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
            Debug.LogError($"ProduceBuilding {name} имеет неверный тип данных!");
            enabled = false;
            return;
        }
        _productionTimer = ProduceData.ProductionInterval; // чтобы первый цикл началс€ сразу
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

    /// <summary>
    /// ѕопытка произвести продукт.
    /// </summary>
    protected virtual void TryProduce()
    {
        if (!HasInputResources()) return;

        // «атраты ресурсов
        if (ProduceData.InputResources.Resources.Count > 0)
        {
            if (!_resourceManager.TrySpendResources(ProduceData.InputResources))
            {
                Debug.Log($"{ProduceData.DisplayName}: недостаточно ресурсов дл€ производства");
                return;
            }
        }

        // ѕроизводство
        if (ProduceData.SpawnsUnits)
        {
            SpawnUnits();
        }
        else
        {
            _resourceManager.AddResources(ProduceData.OutputResources);
        }

        PlayProductionEffects();
    }

    protected virtual bool HasInputResources()
    {
        if (ProduceData.InputResources.Resources.Count == 0)
            return true;
        return _resourceManager.HasResources(ProduceData.InputResources);
    }

    protected virtual void SpawnUnits()
    {
        for (int i = 0; i < ProduceData.UnitsPerCycle; i++)
        {
            Vector3 spawnPos = transform.position;
            if (ProduceData.SpawnPoint != null)
                spawnPos = ProduceData.SpawnPoint.position;
            else
                spawnPos += new Vector3(ProduceData.Width * 0.5f, 0); // например, справа от здани€

            Instantiate(ProduceData.UnitPrefab, spawnPos, Quaternion.identity);
        }
    }

    protected virtual void PlayProductionEffects()
    {
        // јнимации, звуки, частицы Ч можно переопределить в наследниках
        Debug.Log($"{ProduceData.DisplayName} произвЄл продукт.");
    }

    /// <summary>
    /// ќстановить производство (например, при нехватке энергии).
    /// </summary>
    public void SetProduction(bool enabled)
    {
        _isProducing = enabled;
    }
}