using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Сохранение и загрузка игры в JSON-файл в папке сохранений.
/// </summary>
/// <remarks>
/// Перенос из ветки features/13_save_system (коммит 30816d2). Отличия от исходника:
/// убраны мусорные using (UnityEditor, UnityEngine.EventSystems и т.п.), загрузка
/// действительно подключена (у автора LoadGame не вызывался ниоткуда), добавлен
/// отложенный старт с аргументом -load и горячие клавиши, а автоматическое
/// сохранение при выходе убрано — сохранение теперь только явное.
/// </remarks>
public class SavingSystem : IGameSystem
{
    public string SystemName => "Saving System";
    public bool IsActive { get; set; } = true;

    /// <summary>Имя файла. Слотов пока нет: одно сохранение на игру.</summary>
    private const string SaveFileName = "save.json";

    /// <summary>Аргумент запуска: загрузить сохранение, когда мир будет готов.</summary>
    private const string LoadArgument = "-load";

    private GameKernel _kernel;
    private DataCollectorSystem _dataCollector;
    private string _savePath;

    private bool _loadRequested;
    private bool _loadDone;

    /// <summary>Путь к файлу сохранения (нужен для проверок и логов).</summary>
    public string SavePath => _savePath;

    /// <summary>Есть ли уже файл сохранения.</summary>
    public bool HasSave => !string.IsNullOrEmpty(_savePath) && File.Exists(_savePath);

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        _dataCollector = kernel.GetSystem<DataCollectorSystem>();

        string folder = Path.Combine(Application.persistentDataPath, "Saves");

        try
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            _savePath = Path.Combine(folder, SaveFileName);
        }
        catch (Exception e)
        {
            Debug.LogError($"[{SystemName}] Не удалось подготовить папку сохранений {folder}: {e.Message}");
        }

        _loadRequested = HasCommandLineArgument(LoadArgument);

        Debug.Log($"[{SystemName}] Инициализирована. Файл: {_savePath}"
            + (_loadRequested ? " (запрошена загрузка при старте)" : string.Empty));
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        // Ручное сохранение и загрузка: своего UI для этого пока нет.
        if (Input.GetKeyDown(KeyCode.F5)) SaveGame();
        else if (Input.GetKeyDown(KeyCode.F9)) LoadGame();

        // Отложенная загрузка: мир создаётся позже, чем инициализируется система,
        // и загружать в пустой мир бессмысленно.
        if (_loadRequested && !_loadDone && IsWorldReady())
        {
            _loadDone = true;
            LoadGame();
        }
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown() { }

    /// <summary>Сохраняет мир в файл.</summary>
    /// <returns>True, если файл записан.</returns>
    public bool SaveGame()
    {
        if (string.IsNullOrEmpty(_savePath))
        {
            Debug.LogError($"[{SystemName}] Путь сохранения недоступен, сохранить нельзя.");
            return false;
        }

        if (_dataCollector == null)
        {
            Debug.LogError($"[{SystemName}] DataCollector не найден: сохранять нечего.");
            return false;
        }

        SaveData data = _dataCollector.GetData<SaveData>();
        if (data == null)
        {
            Debug.LogWarning($"[{SystemName}] Провайдер SaveData ничего не вернул: мир ещё не создан.");
            return false;
        }

        try
        {
            File.WriteAllText(_savePath, JsonUtility.ToJson(data, true));
            Debug.Log($"[{SystemName}] Игра сохранена: сид {data.seed}, изменённых блоков {data.changedBlocks.Count}. Файл: {_savePath}");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[{SystemName}] Ошибка сохранения: {e.Message}");
            return false;
        }
    }

    /// <summary>Загружает мир из файла.</summary>
    /// <returns>True, если данные применены.</returns>
    public bool LoadGame()
    {
        if (!HasSave)
        {
            Debug.Log($"[{SystemName}] Файл сохранения не найден: {_savePath}");
            return false;
        }

        if (_dataCollector == null)
        {
            Debug.LogError($"[{SystemName}] DataCollector не найден: загружать некуда.");
            return false;
        }

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(File.ReadAllText(_savePath));
        }
        catch (Exception e)
        {
            Debug.LogError($"[{SystemName}] Не удалось прочитать сохранение: {e.Message}");
            return false;
        }

        if (data == null)
        {
            Debug.LogError($"[{SystemName}] Сохранение пустое или повреждено.");
            return false;
        }

        if (!_dataCollector.RestoreData(data))
        {
            return false;
        }

        Debug.Log($"[{SystemName}] Игра загружена: сид {data.seed}, изменённых блоков {data.changedBlocks?.Count ?? 0}.");
        return true;
    }

    /// <summary>Мир готов к загрузке: карта создана и провайдер данных зарегистрирован.</summary>
    private bool IsWorldReady()
    {
        if (_dataCollector == null || !_dataCollector.HasProvider<SaveData>()) return false;
        if (!_kernel.TryGetSystem(out WorldSystem worldSystem)) return false;

        return worldSystem.WorldMap != null;
    }

    private static bool HasCommandLineArgument(string argument)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], argument, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}
