using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Система миникарты. Генерирует и обновляет Texture2D на основе событий карты.
/// </summary>
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
        { BlockType.Air, new Color(0.1f, 0.1f, 0.1f, 1f) },
        { BlockType.Rock, new Color(0.5f, 0.5f, 0.5f, 1f) },
        { BlockType.Minerals, Color.cyan },
        { BlockType.Ice, new Color(0.3f, 0.8f, 1f, 1f) },
        { BlockType.Root, Color.green }
    };

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        // Подписываемся на события генерации
        _kernel.EventBus.Subscribe<ChunkGeneratedEvent>(OnChunkGenerated);
        _kernel.EventBus.Subscribe<BlockChangedEvent>(OnBlockChanged);
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        if (MapTexture == null)
        {
            TryCreateTexture();
        }

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
        if (_kernel != null)
        {
            _kernel.EventBus.Unsubscribe<ChunkGeneratedEvent>(OnChunkGenerated);
            _kernel.EventBus.Unsubscribe<BlockChangedEvent>(OnBlockChanged);
        }

        if (MapTexture != null) Object.Destroy(MapTexture);
    }

    /// <summary>
    /// Создает текстуру миникарты, как только WorldMap становится доступен.
    /// </summary>
    private bool TryCreateTexture()
    {
        var world = _kernel.GetSystem<WorldSystem>();
        if (world == null || world.WorldMap == null) return false;

        int width = world.WorldMap.Width;
        int height = world.WorldMap.Height;

        if (width <= 0 || height <= 0) return false;

        MapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        MapTexture.filterMode = FilterMode.Point; // Четкие пиксели без размытия

        // Изначально заливаем черным (Неизведанная территория)
        Color[] fillPixels = new Color[width * height];
        for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = Color.black;
        MapTexture.SetPixels(fillPixels);
        MapTexture.Apply();

        Debug.Log($"[{SystemName}] Текстура миникарты успешно создана ({width}x{height}).");
        return true;
    }

    // --- Обработка событий ---

    private void OnChunkGenerated(ChunkGeneratedEvent evt)
    {
        if (MapTexture == null)
        {
            if (!TryCreateTexture()) return;
        }

        var world = _kernel.GetSystem<WorldSystem>();
        if (world == null) return;

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
        if (MapTexture == null) return;

        Color pixelColor = _colorMap.ContainsKey(evt.NewType) ? _colorMap[evt.NewType] : Color.magenta;
        MapTexture.SetPixel(evt.X, evt.Y, pixelColor);
        _needsApply = true;
    }
}