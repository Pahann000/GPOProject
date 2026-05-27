public struct ResourceChangedEvent : IGameEvent
{
	public ResourceType Type;
	public int NewAmount;
	public int Delta;
	public FactionType Faction;

	public ResourceChangedEvent(ResourceType type, int newAmount, int delta, FactionType faction)
	{
		Type = type;
		NewAmount = newAmount;
		Delta = delta;
		Faction = faction;
	}
}
