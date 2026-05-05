using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Главный контейнер всех игровых данных для сериализации.
/// </summary>
[Serializable]
public class SaveData
{
    // Данные мира (карта)
    public string seedString;
    public int worldWidth;
    public int worldHeight;
    public int chunkSize;

    // Измененные блоки 
    // Пока не тестировалось потому что блоки не ломаются не ставятся почему то (так и было) /ᐠ - ˕ -マ Ⳋ
    public List<BlockChangeData> changedBlocks = new List<BlockChangeData>();

    // Данные ресурсов wip
    // public Dictionary<string, int> resources = new Dictionary<string, int>();  
}

/// <summary>
/// Данные об измененном блоке.
/// </summary>
[Serializable]
public class BlockChangeData
{
    public int x;
    public int y;
    public BlockType type;
}