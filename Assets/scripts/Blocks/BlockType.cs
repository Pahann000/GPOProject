using UnityEngine;

[CreateAssetMenu(fileName = "TileClass", menuName = "Tile Class")]
public class BlockType : ScriptableObject
{
    public Sprite Sprite;
    public string Name;
    public int Hardness = 1;
}
