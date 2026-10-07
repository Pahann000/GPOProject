/// <summary>
/// Определяет все типы ресурсов в игре.
/// Значения заданы явно: enum уходит по сети (ResourceChangedEvent в TargetRpc)
/// и хранится в ScriptableObject-ассетах.
/// </summary>
public enum ResourceType
{
    Minerals = 0,
    Ice = 1,
    Rock = 2,
    Root = 3,
    Energy = 4
}
