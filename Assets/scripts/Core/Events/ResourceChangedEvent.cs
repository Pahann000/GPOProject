public struct ResourceChangedEvent : IGameEvent
{
	public ResourceType Type;
	public int NewAmount;
	public int Delta;
	public Player Player;

	public ResourceChangedEvent(Player player, ResourceType type, int newAmount, int delta)
	{
		Player = player;
		Type = type;
		NewAmount = newAmount;
		Delta = delta;
	}
}
