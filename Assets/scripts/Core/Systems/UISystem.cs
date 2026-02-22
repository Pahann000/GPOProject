using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Отвечает за инициализацию пользовательского интерфейса.
/// Динамически загружает префаб UI из папки Resources при старте игры.
/// Устраняет необходимость хранить Canvas на игровой сцене.
/// </summary>
public class UISystem : IGameSystem
{
    public string SystemName => "UI System";
    public bool IsActive { get; set; } = true;

    public GameKernel _kernel;
    public GameObject _uiInstance;

    public UIManager Manager { get; private set; }

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        CreateEventSystem();

        SpawnUIPrefab();

        Debug.Log($"[{SystemName}] Динамически создана и инициализирована.");
    }

    public void Tick(float deltaTime)
    {
        // Можно будет обработать горячие клавиши тут. Пока лень
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        if (_uiInstance != null)
        {
            Object.Destroy(_uiInstance);
        }
    }

    private void CreateEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            Object.DontDestroyOnLoad(es);
        }
    }

    private void SpawnUIPrefab()
    {
        GameObject prefab = Resources.Load<GameObject>("MainUIPrefab");

        if (prefab == null)
        {
            Debug.LogError($"[{SystemName}] КРИТИЧЕСКАЯ ОШИБКА: Префаб 'MainUIPrefab' не найден в папке Prefab/UI!");
            return;
        }

        _uiInstance = Object.Instantiate(prefab);
        _uiInstance.name = "UI_Root_Canvas";

        Object.DontDestroyOnLoad(_uiInstance);

        Manager = _uiInstance.GetComponent<UIManager>();

        if (Manager == null)
        {
            Debug.LogWarning($"[{SystemName}] На префабе 'MainUIPrefab' нет компонента UIManager!");
        }
    }
}