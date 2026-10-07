/// <summary>
/// Названия блоков.
/// Значения заданы явно: enum уходит по сети (RpcPlaceBlock), и вставка
/// нового значения в середину списка сломала бы протокол и маппинг атласа.
/// </summary>
public enum BlockType 
{
    Air = 0,
    Dirt = 1,
    Stone = 2,
    Gold = 3,
    Minerals = 4,
    Ice = 5,
    Root = 6,
}
