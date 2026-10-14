using System;
using System.Collections.Generic;

/// <summary>
/// Контейнер сохраняемых данных мира.
/// </summary>
/// <remarks>
/// Перенос из ветки features/13_save_system (коммит 30816d2). Хранится не вся карта,
/// а только то, что нельзя восстановить из сида: сам сид и правки блоков.
/// </remarks>
[Serializable]
public class SaveData
{
    /// <summary>Сид мира: по нему карта восстанавливается детерминированно.</summary>
    public float seed;

    /// <summary>Размеры мира в блоках: нужны, чтобы предупредить о несовпадении конфигурации.</summary>
    public int worldWidth;
    public int worldHeight;

    /// <summary>Размер чанка на момент сохранения.</summary>
    public int chunkSize;

    /// <summary>Блоки, которые игрок поставил или сломал.</summary>
    public List<BlockChangeData> changedBlocks = new List<BlockChangeData>();
}

/// <summary>
/// Один изменённый блок: координаты и итоговый тип.
/// </summary>
[Serializable]
public class BlockChangeData
{
    public int x;
    public int y;
    public BlockType type;
}
