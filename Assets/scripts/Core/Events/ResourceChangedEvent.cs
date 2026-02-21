public struct ResourceChangedEvent : IGameEvent
{
	public ResourceType Type;
	public int NewAmount;
	public int Delta;

	public ResourceChangedEvent(ResourceType type, int newAmount, int delta)
	{
		Type = type;
		NewAmount = newAmount;
		Delta = delta;
	}
}
