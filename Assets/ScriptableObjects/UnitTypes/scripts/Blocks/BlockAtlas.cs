using UnityEngine;

[CreateAssetMenu(fileName = "TileAtlas", menuName = "Tile Atlas")]
public class BlockAtlas : ScriptableObject
{
    public BlockType Minerals;
    public BlockType Rock;
    public BlockType Ice;
    public BlockType Root;
}
