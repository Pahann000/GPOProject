using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Хранит информацию о всех существующих блоках.
/// </summary>
[System.Serializable]
[CreateAssetMenu(menuName = "Block Atlas")]
public class BlockAtlas : ScriptableObject
{
    [System.Serializable]
    private struct BlockInfo
    {
        public BlockType type;
        public Vector2 uvPosition;
        public Vector2 uvSize;
    }

    [SerializeField]
    private List<BlockInfo> blocks;

    public Rect GetUV(BlockType type)
    {
        foreach (BlockInfo info in blocks)
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
