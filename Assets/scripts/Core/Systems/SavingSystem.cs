using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEditor.Overlays;
using UnityEngine;
using static UnityEngine.Rendering.STP;
using UnityEngine.EventSystems;

/// <summary>
/// Управляет сохранением игры при выходе.
/// </summary>
public class SavingSystem : IGameSystem
{
    public string SystemName => "Saving System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private string savePath;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        // Инициализируем папку сохранений
        string folderPath = Path.Combine(Application.persistentDataPath, "Saves");

        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            savePath = Path.Combine(folderPath, "ignoreName.json");
            Debug.Log($"[SaveSystem] Путь сохранения: {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Ошибка доступа к {folderPath}: {e.Message}");
        }

        Debug.Log($"[{SystemName}] Инициализирована. Путь: {savePath}");
    }

    public void Tick(float deltaTime) { }
    public void FixedTick(float deltaTime) { }

    public void Shutdown()
    {
        SaveGame();
    }

    /// <summary>
    /// Сохранить игру
    /// </summary>
    public void SaveGame()
    {
        try
        {
            SaveData saveData = new SaveData();

            // Собираем данные с систем
            saveData = CaptureWorldData();

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(savePath, json);

            Debug.Log($"[{SystemName}] Игра сохранена в {savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{SystemName}] Ошибка сохранения: {e.Message}");
        }
    }

    /// <summary>
    /// Загрузить игру
    /// </summary>
    public void LoadGame(string saveName)
    {
        // ฅ^•ﻌ•^ฅ
    }

    /// <summary>
    /// Внутренний метод, собрирающий данные о мире.
    /// </summary>
    private SaveData CaptureWorldData()
    {
        var captureEvent = new CaptureWorldDataEvent();
        _kernel.EventBus.Raise(captureEvent);
        return captureEvent.Result ?? new SaveData();
    }

    /// <summary>
    /// Внутренний метод, восстановливающий данные о мире.
    /// </summary>
    private void RestoreWorldData(SaveData data)
    {
        if (data == null) return;
        _kernel.EventBus.Raise(new RestoreWorldDataEvent(data));
    }
}
