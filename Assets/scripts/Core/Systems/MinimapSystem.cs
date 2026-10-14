using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Клиентская система миникарты: рисует карту мира в текстуру по мере появления
/// чанков и обновляет отдельные пиксели при постановке и разрушении блоков.
/// </summary>
/// <remarks>
/// Перенос из ветки features/12_minimap (коммит f9a3913). Отличия от исходника:
/// текстура уменьшается относительно мира (иначе 1600x1600 блоков дают 10 МБ и
/// постоянные полные выгрузки в GPU), создание текстуры отложено до появления
/// карты на клиенте и уже загруженные чанки дорисовываются при создании,
/// а UI строится кодом, без правки префаба MainUIPrefab.
/// </remarks>
public class MinimapSystem : IGameSystem
{
    public string SystemName => "Minimap System";

    public bool IsActive { get; set; } = true;

    /// <summary>Максимальный размер текстуры по большей стороне мира.</summary>
    private const int MaxTextureSize = 512;

    /// <summary>Цвета блоков на миникарте.</summary>
    private static readonly Dictionary<BlockType, Color> Colors = new Dictionary<BlockType, Color>
    {
        { BlockType.Air, new Color(0.10f, 0.10f, 0.12f, 1f) },
        { BlockType.Stone, new Color(0.50f, 0.50f, 0.52f, 1f) },
        { BlockType.Dirt, new Color(0.40f, 0.24f, 0.10f, 1f) },
        { BlockType.Gold, new Color(1f, 0.84f, 0.20f, 1f) },
        { BlockType.Minerals, new Color(0.20f, 0.85f, 0.90f, 1f) },
        { BlockType.Ice, new Color(0.35f, 0.80f, 1f, 1f) },
        { BlockType.Root, new Color(0.35f, 0.85f, 0.35f, 1f) },
    };

    private GameKernel _kernel;
    private Map _map;
    private int _scale = 1;
    private bool _needsApply;
    private MinimapUI _ui;

    /// <summary>Чанки, уже перенесённые в текстуру: сверка не даёт потерять их при любом порядке событий.</summary>
    private readonly HashSet<Vector2Int> _drawnChunks = new HashSet<Vector2Int>();
    private float _reconcileTimer;
    private const float ReconcileInterval = 0.5f;

    /// <summary>Готовая текстура миникарты (её показывает <see cref="MinimapUI"/>).</summary>
    public Texture2D MapTexture { get; private set; }

    /// <summary>Сколько блоков мира приходится на один пиксель миникарты.</summary>
    public int PixelScale => _scale;

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;

        kernel.EventBus.Subscribe<ChunkGeneratedEvent>(OnChunkGenerated);
        kernel.EventBus.Subscribe<BlockChangedEvent>(OnBlockChanged);
    }

    public void Tick(float deltaTime)
    {
        if (!IsActive) return;

        // Карта появляется на клиенте после синхронизации сида, поэтому текстуру
        // приходится создавать отложенно, когда мир уже есть.
        if (MapTexture == null)
        {
            TryCreateTexture();
            return;
        }

        // Пиксели применяем один раз за кадр, а не на каждый чанк.
        if (_needsApply)
        {
            MapTexture.Apply(false);
            _needsApply = false;
        }

        // Сверка на случай, если чанк появился раньше текстуры или мир был пересоздан.
        _reconcileTimer -= deltaTime;
        if (_reconcileTimer <= 0f)
        {
            _reconcileTimer = ReconcileInterval;
            ReconcileChunks();
        }

        TryCreateUi();
    }

    public void FixedTick(float fixedDeltaTime) { }

    public void Shutdown()
    {
        if (_kernel != null)
        {
            _kernel.EventBus.Unsubscribe<ChunkGeneratedEvent>(OnChunkGenerated);
            _kernel.EventBus.Unsubscribe<BlockChangedEvent>(OnBlockChanged);
        }

        if (_ui != null)
        {
            Object.Destroy(_ui.gameObject);
            _ui = null;
        }

        if (MapTexture != null)
        {
            Object.Destroy(MapTexture);
            MapTexture = null;
        }
    }

    /// <summary>
    /// Сколько блоков мира кладём в один пиксель миникарты, чтобы текстура
    /// не превышала <see cref="MaxTextureSize"/> по большей стороне.
    /// </summary>
    public static int GetPixelScale(int worldWidth, int worldHeight)
    {
        int largest = Mathf.Max(worldWidth, worldHeight);
        if (largest <= 0) return 1;

        return Mathf.Max(1, Mathf.CeilToInt(largest / (float)MaxTextureSize));
    }

    /// <summary>Переводит координаты блока в координаты пикселя миникарты.</summary>
    public static Vector2Int WorldToPixel(int x, int y, int scale)
    {
        int safeScale = scale > 0 ? scale : 1;
        return new Vector2Int(x / safeScale, y / safeScale);
    }

    /// <summary>Цвет блока на миникарте. Неизвестные типы красим в magenta, чтобы их было видно.</summary>
    public static Color GetColor(BlockType type)
    {
        return Colors.TryGetValue(type, out Color color) ? color : Color.magenta;
    }

    private void TryCreateTexture()
    {
        if (!_kernel.TryGetSystem(out WorldSystem worldSystem) || worldSystem.WorldMap == null) return;

        // Мир мог быть пересоздан (новый сид) — старую текстуру и отметки выбрасываем.
        if (_map != null && _map != worldSystem.WorldMap)
        {
            if (MapTexture != null) Object.Destroy(MapTexture);
            MapTexture = null;
            _drawnChunks.Clear();
        }

        _map = worldSystem.WorldMap;
        _scale = GetPixelScale(_map.Width, _map.Height);

        int width = Mathf.Max(1, _map.Width / _scale);
        int height = Mathf.Max(1, _map.Height / _scale);

        MapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            name = "MinimapTexture"
        };

        // Неизведанная территория — чёрная: чанки дорисуются по мере появления.
        Color[] fill = new Color[width * height];
        for (int i = 0; i < fill.Length; i++) fill[i] = Color.black;
        MapTexture.SetPixels(fill);
        MapTexture.Apply(false);

        Debug.Log($"[{SystemName}] Текстура миникарты создана: {width}x{height} (1 пиксель = {_scale} бл.).");

        // Чанки могли появиться раньше текстуры — дорисовываем то, что уже загружено.
        ReconcileChunks();

        if (_needsApply)
        {
            MapTexture.Apply(false);
            _needsApply = false;
        }
    }

    /// <summary>
    /// Дорисовывает чанки, которых ещё нет в текстуре. Нужна потому, что порядок
    /// событий не гарантирован: чанк может появиться раньше самой текстуры.
    /// </summary>
    private void ReconcileChunks()
    {
        if (_map == null || MapTexture == null) return;

        foreach (Vector2Int chunkPos in _map.LoadedChunks)
        {
            if (_drawnChunks.Contains(chunkPos)) continue;

            DrawChunk(chunkPos.x, chunkPos.y, _map.ChunkSize);
        }
    }

    private void TryCreateUi()
    {
        if (_ui != null) return;
        if (!_kernel.TryGetSystem(out UISystem uiSystem) || uiSystem._uiInstance == null) return;

        _ui = MinimapUI.Create(uiSystem._uiInstance.transform, MapTexture);
    }

    private void OnChunkGenerated(ChunkGeneratedEvent evt)
    {
        if (!IsActive || MapTexture == null) return;

        DrawChunk(evt.ChunkX, evt.ChunkY, evt.ChunkSize);
    }

    private void DrawChunk(int chunkX, int chunkY, int chunkSize)
    {
        if (_map == null || MapTexture == null) return;

        int startX = chunkX * chunkSize;
        int startY = chunkY * chunkSize;

        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                int worldX = startX + x;
                int worldY = startY + y;

                SetPixel(worldX, worldY, _map.GetBlockType(worldX, worldY));
            }
        }

        _drawnChunks.Add(new Vector2Int(chunkX, chunkY));
        _needsApply = true;
    }

    private void OnBlockChanged(BlockChangedEvent evt)
    {
        if (!IsActive || MapTexture == null) return;

        SetPixel(evt.X, evt.Y, evt.NewType);
        _needsApply = true;
    }

    private void SetPixel(int worldX, int worldY, BlockType type)
    {
        if (worldX < 0 || worldY < 0) return;

        Vector2Int pixel = WorldToPixel(worldX, worldY, _scale);
        if (pixel.x >= MapTexture.width || pixel.y >= MapTexture.height) return;

        MapTexture.SetPixel(pixel.x, pixel.y, GetColor(type));
    }
}
