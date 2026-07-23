using System.Collections.Generic;
using UnityEngine;

public class MinimapSystem : IGameSystem
{
    public string SystemName => "Minimap System";
    public bool IsActive { get; set; } = true;

    private GameKernel _kernel;

    // Сама текстура миникарты, которую мы передадим в UI
    public Texture2D MapTexture { get; private set; }

    // Флаг: нужно ли применить изменения к текстуре в этом кадре?
    private bool _needsApply = false;

    // Цвета для разных типов блоков на миникарте
    private Dictionary<BlockType, Color> _colorMap = new Dictionary<BlockType, Color>()
    {
        { BlockType.Air, new Color(0.1f, 0.1f, 0.1f, 1f) }, // Темно-серый фон для раскопанного воздуха
        { BlockType.Stone, new Color(0.5f, 0.5f, 0.5f, 1f) },
        { BlockType.Dirt, new Color(0.4f, 0.2f, 0f, 1f) },
        { BlockType.Gold, Color.yellow },
        { BlockType.Minerals, Color.cyan },
        { BlockType.Ice, new Color(0.3f, 0.8f, 1f, 1f) },
        { BlockType.Root, Color.green }
    };

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        var world = _kernel.GetSystem<WorldSystem>();
        if (world == null || world.WorldMap == null) return;

        int width = world.WorldMap.Width;
        int height = world.WorldMap.Height;

        // Создаем текстуру размером с мир
        MapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        MapTexture.filterMode = FilterMode.Point; // Делаем пиксели четкими, без размытия

        // Изначально заливаем всё черным цветом (Неизведанная территория)
        Color[] fillPixels = new Color[width * height];
        for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = Color.black;
        MapTexture.SetPixels(fillPixels);
        MapTexture.Apply();

        // Подписываемся на события генерации
        _kernel.EventBus.Subscribe<ChunkGeneratedEvent>(OnChunkGenerated);
        _kernel.EventBus.Subscribe<BlockChangedEvent>(OnBlockChanged);

        Debug.Log($"[{SystemName}] Миникарта инициализирована ({width}x{height}).");
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        // Применяем изменения пикселей батчем 1 раз за кадр (Оптимизация)
        if (_needsApply)
        {
            MapTexture.Apply();
            _needsApply = false;
        }
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        _kernel.EventBus.Unsubscribe<ChunkGeneratedEvent>(OnChunkGenerated);
        _kernel.EventBus.Unsubscribe<BlockChangedEvent>(OnBlockChanged);
        if (MapTexture != null) Object.Destroy(MapTexture);
    }

    // --- Обработка событий ---

    private void OnChunkGenerated(ChunkGeneratedEvent evt)
    {
        var world = _kernel.GetSystem<WorldSystem>();

        int startX = evt.ChunkX * evt.ChunkSize;
        int startY = evt.ChunkY * evt.ChunkSize;

        // Проходим по каждому блоку в загруженном чанке
        for (int x = 0; x < evt.ChunkSize; x++)
        {
            for (int y = 0; y < evt.ChunkSize; y++)
            {
                int worldX = startX + x;
                int worldY = startY + y;

                BlockType type = world.GetBlockTypeAt(worldX, worldY);
                Color pixelColor = _colorMap.ContainsKey(type) ? _colorMap[type] : Color.magenta;

                MapTexture.SetPixel(worldX, worldY, pixelColor);
            }
        }

        _needsApply = true;
    }

    private void OnBlockChanged(BlockChangedEvent evt)
    {
        Color pixelColor = _colorMap.ContainsKey(evt.NewType) ? _colorMap[evt.NewType] : Color.magenta;
        MapTexture.SetPixel(evt.X, evt.Y, pixelColor);
        _needsApply = true;
    }
}