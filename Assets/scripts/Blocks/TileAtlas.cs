using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Tile Atlas")]
public class TileAtlas : ScriptableObject
{
    [System.Serializable]
    public struct TileInfo
    {
        public TileType type;
        public Vector2 uvPosition;
        public Vector2 uvSize;
    }

    public List<TileInfo> tiles;

    public Rect GetUV(TileType type)
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