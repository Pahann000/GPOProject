using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Block Atlas")]
public class BlockAtlas : ScriptableObject
{
    [System.Serializable]
    public struct TileInfo
    {
        public BlockType type;
        public Vector2 uvPosition;
        public Vector2 uvSize;
    }

    public List<TileInfo> tiles;

    public Rect GetUV(BlockType type)
    {
        foreach (TileInfo info in tiles)
        {
            if (info.type == type)
            {
                return new Rect(
                    info.uvPosition.x,
                    info.uvPosition.y,
                    info.uvSize.x,
                    info.uvSize.y
                );
            }
        }
        return Rect.zero;
    }
}