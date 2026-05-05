using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEngine.Rendering.DebugUI;
using static UnityEngine.Rendering.STP;

/// <summary>
/// Управляет сохранением игры при выходе.
/// </summary>
public class SavingSystem : IGameSystem
{
    public string SystemName => "Saving System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;
    private string _savePath;
    private DataCollectorSystem _dataCollector;

    public WorldSystem _worldSystem;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        _dataCollector = kernel.GetSystem<DataCollectorSystem>();

        // Инициализируем папку сохранений
        string folderPath = Path.Combine(Application.persistentDataPath, "Saves");

        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            _savePath = Path.Combine(folderPath, "ignoreName.json");
            Debug.Log($"[SaveSystem] Путь сохранения: {_savePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Ошибка доступа к {folderPath}: {e.Message}");
        }

        Debug.Log($"[{SystemName}] Инициализирована. Путь: {_savePath}");

    }

    public void Tick(float deltaTime) { }
    public void FixedTick(float deltaTime) { }

    public void Shutdown()
    {
        SaveGame();
    }

    /// <summary>
    /// Сохраяет игру.
    /// </summary>
    public void SaveGame()
    {
        try
        {
            if (_dataCollector == null)
            {
                Debug.LogError("[SavingSystem] DataCollector не найден!");
                return;
            }

            // Получаем данные через DataCollector
            var worldData = _dataCollector.GetData<SaveData>();

            if (worldData == null)
            {
                Debug.LogError("[SavingSystem] Не удалось получить данные мира!");
                return;
            }

            string json = JsonUtility.ToJson(worldData, true);
            File.WriteAllText(_savePath, json);

            Debug.Log($"[{SystemName}] Игра сохранена: seed={worldData.seedString}, changedBlocks={worldData.changedBlocks.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[{SystemName}] Ошибка сохранения: {e.Message}");
        }
    }

    /// <summary>
    /// Загружает игру.
    /// Пока не знаю куда впихнуть да и не готово оно
    /// </summary>
    public void LoadGame(string saveName)
    {
        if (!File.Exists(_savePath))
        {
            Debug.Log($"[{SystemName}] Файл сохранения не найден");
            return;
        }

        string json = File.ReadAllText(_savePath);
        var gameSaveData = JsonUtility.FromJson<SaveData>(json);

        if (gameSaveData == null) return;

        if (_dataCollector != null)
        {
            _dataCollector.RestoreData(gameSaveData);
        }

        Debug.Log($"[{SystemName}] Игра загружена");
    }
}
