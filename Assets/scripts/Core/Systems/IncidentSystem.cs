using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class IncidentSystem : IGameSystem
{
    public string SystemName => "Incident System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private List<GameIncident> _incidents;

    private float _timer;
    private float _checkInterval = 5f; // Проверка на событие каждые 5 минут.
    private float _globalChance = 0.4f; // 40% шанс, что при проверке что-то случится.

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        _incidents = Resources.LoadAll<GameIncident>("Incidents").ToList();

        _timer = _checkInterval;

        Debug.Log($"[{SystemName}] Запущена. Найдено событий: {_incidents.Count}");
    }

    public void Tick(float deltaTime)
    {
        _timer -= deltaTime;
        if (_timer < 0 )
        {
            _timer = _checkInterval;
            TryTriggerRandomIncident();
        }
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown() { }

    private void TryTriggerRandomIncident()
    {
        if (_incidents.Count == 0) return;

        if (Random.value > _globalChance) return;

        var validIncidents = _incidents.Where(e => e.CanTrigger(_kernel)).ToList();
        if (validIncidents.Count == 0) return;

        float totalWeight = validIncidents.Sum(e => e.Weight);
        float roll = Random.Range(0, totalWeight);
        float currentSum = 0;

        foreach (var inc in validIncidents)
        {
            currentSum += inc.Weight;
            if (roll <= currentSum)
            {
                TriggerIncident(inc);
                break;
            }
        }
    }

    private void TriggerIncident(GameIncident inc)
    {
        Debug.Log($"<color=red>[INCIDENT]</color> Произошёл инцидент: {inc.DisplayName}");

        inc.Execute(_kernel);

        // _kernel.EventBus.Raise(new IncidentTriggeredEvent(inc);
    }
}