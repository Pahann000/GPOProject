public class FogOfWarSystem : IGameSystem
{
    public string SystemName => "Fog Of War";
    public bool IsActive { get; set; } = true;
    private GameKernel _kernel;
    private Dictionary<FactionType, bool[,]> _visible;  // для каждой фракции карта видимости

    public void Initialize(GameKernel kernel)
    {
        _kernel = kernel;
        // Получить размеры карты из WorldSystem
        var world = _kernel.GetSystem<WorldSystem>();
        int width = world.WorldMap.Width;
        int height = world.WorldMap.Height;
        _visible = new Dictionary<FactionType, bool[,]>();
        foreach (FactionType f in System.Enum.GetValues(typeof(FactionType)))
            _visible[f] = new bool[width, height];
    }

    public void Tick(float deltaTime)
    {
        // Очистить видимость
        foreach (var vis in _visible.Values)
            System.Array.Clear(vis, 0, vis.Length);
        // Для каждой фракции, для каждого её юнита/здания закрасить область видимости
        // Используя радиус обзора (можно добавить поле в Unit/Building)
    }

    public bool IsVisible(FactionType observer, int x, int y) => _visible[observer][x, y];
    public void RevealForFaction(FactionType faction, int x, int y, int radius) { /* закрасить круг */ }
}