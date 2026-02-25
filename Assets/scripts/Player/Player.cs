using UnityEngine;

/// <summary>
/// Класс, обеспечивающий взаимодействие игрока с игровым миром.
/// </summary>
public class Player : MonoBehaviour, IChunkObserver
{
    private UnitController _unitController = new();

    private void Start() => ChunkManager.Instance.RegisterObserver(this);
    private void OnDestroy() => ChunkManager.Instance.UnregisterObserver(this);

    /// <summary>
    /// Атлас со всеми видами юнитов.
    /// </summary>
    [SerializeField]private UnitAtlas unitAtlas;

    //[SerializeField] private BuildingAtlas buildingAtlas;

    /// <summary>
    /// временная реализация инвентаря игрока.
    /// </summary>
    public ObservableDictionary<string, int> Resources { get; } = new ObservableDictionary<string, int>();

    /// <summary>
    /// Положение игрока по X.
    /// </summary>
    public int X => (int)this.transform.position.x;

    /// <summary>
    /// Положение игрока по X.
    /// </summary>
    public int Y => (int)this.transform.position.y;

    /// <inheritdoc/>
    void Awake()
    {
        Resources.Add("Gold", 10);
        _unitController.PlaceUnit(unitAtlas.Miner, this, new Vector2(10, 150));
        _unitController.PlaceUnit(unitAtlas.Miner, this, new Vector2(11, 150));
        _unitController.PlaceUnit(unitAtlas.Soldier, this, new Vector2(12, 151));
    }

    /// <inheritdoc/>
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mousePos = Camera.main.ScreenPointToRay(Input.mousePosition);
            SelectTarget(new Vector2(mousePos.origin.x, mousePos.origin.y));
        }

        _unitController.CheckActiveTasks();
        _unitController.DistributeTasks();
    }

    //TODO: сделать нормальное выделение, и вообще работу игрока с блоками
    /// <summary>
    /// выделяет блок(заглушка заставляющая юнита уничтожить блок).
    /// </summary>
    /// <param name="position">Позиция выделяемого блока.</param>
    private void SelectTarget(Vector2 position)
    {
        IDamagable target = null;
        Block selectedBlock = Map.Instance.GetBlockObj(((int)position.x), ((int)position.y));

        if (selectedBlock.tileData.type != BlockType.Air)
        {
            target = selectedBlock;
            _unitController.AddTask(target, UnitTypeName.Miner);
        }
        else
        {
            Vector3 vector3 = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var targetCollider = Physics2D.OverlapPoint(new Vector2(vector3.x, vector3.y));
            if (targetCollider) 
            {
                Unit unit = targetCollider.gameObject.GetComponent<Unit>();
                if (unit != null && !(unit.Owner == this))
                {
                    target = unit;
                    _unitController.AddTask(target, UnitTypeName.Soldier);
                }
                else
                {
                    return;
                }
            }
        }

    }
}
