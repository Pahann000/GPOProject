using UnityEngine;

/// <summary>
/// Класс игрока.
/// </summary>
public class Player : MonoBehaviour
{
    /// <summary>
    /// Атлас со всеми видами юнитов.
    /// </summary>
    [SerializeField]private UnitAtlas unitAtlas;
    //[SerializeField] private BuildingAtlas buildingAtlas;
    public ObservableDictionary<string, int> Resources { get; } = new ObservableDictionary<string, int>();
    private UnitController _unitController = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Resources.Add("Gold", 10);
        _unitController.PlaceUnit(unitAtlas.Miner, this, new Vector2(10, 300));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
            SelectBlock(new Vector2(mousePos.origin.x, mousePos.origin.y));
        }
    }

    //TODO: сделать нормальное выделение, и вообще работу игрока с блоками
    /// <summary>
    /// выделяет блок(заглушка заставляющая первого юнита уничтожить блок).
    /// </summary>
    /// <param name="position">Позиция выделяемого блока.</param>
    private void SelectBlock(Vector2 position)
    {
        Debug.Log($"clicked at {position.ToString()}");
        Tile selectedTile = Map.Instance.GetTile(((int)position.x), ((int)position.y));
        Debug.Log(selectedTile.tileData.type.ToString());
        if (selectedTile.tileData.type != TileType.Air)
        {
            _unitController.CommandUnit(selectedTile);
        }
    }
}
