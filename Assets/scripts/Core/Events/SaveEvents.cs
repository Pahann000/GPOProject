using UnityEngine;

/// <summary>
/// Событие сохранения игры
/// </summary>
public struct SaveGameEvent : IGameEvent
{
    public string SaveName;

    public SaveGameEvent(string saveName)
    {
        SaveName = saveName;
    }
}

/// <summary>
/// Событие загрузки игры
/// </summary>
public struct LoadGameEvent : IGameEvent
{
    public string SaveName;

    public LoadGameEvent(string saveName)
    {
        SaveName = saveName;
    }
}

/// <summary>
/// Событие для сбора данных о мире
/// </summary>
public struct CaptureWorldDataEvent : IGameEvent
{
    public SaveData Result;
}

/// <summary>
/// Событие восстановления мира
/// </summary>
public struct RestoreWorldDataEvent : IGameEvent
{
    public SaveData Data;
    public RestoreWorldDataEvent(SaveData data) => Data = data;
}
