public struct IncidentTriggeredEvent : IGameEvent
{
    public GameIncident Incident;

    public IncidentTriggeredEvent(GameIncident incident)
    {
        Incident = incident;
    }
}