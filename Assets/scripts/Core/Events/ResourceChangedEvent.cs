public struct ResourceChangedEvent : IGameEvent
{
	public ResourceType Type;
	public int NewAmount;
	public int Limit;
	public int Delta;
	public Player Player;

	/// <summary>
	/// Событие поднято локально на клиенте (синхронизация с сервера).
	/// Такие события нельзя отправлять обратно на сервер — иначе в режиме Host
	/// получается бесконечный цикл «событие → RPC → событие».
	/// </summary>
	public bool Local;

	public ResourceChangedEvent(Player player, ResourceType type, int newAmount, int delta, int limit)
	{
		Player = player;
		Type = type;
		NewAmount = newAmount;
		Delta = delta;
		Limit = limit;
		Local = false;
	}

	/// <summary>
	/// Событие о локально применённом изменении: UI обновляется, но на сервер
	/// оно не пересылается.
	/// </summary>
	public static ResourceChangedEvent CreateLocal(Player player, ResourceType type, int newAmount, int delta, int limit)
	{
		ResourceChangedEvent evt = new ResourceChangedEvent(player, type, newAmount, delta, limit);
		evt.Local = true;
		return evt;
	}
}
