using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField]private UnitAtlas unitAtlas;
    //[SerializeField] private BuildingAtlas buildingAtlas;
    public ObservableDictionary<string, int> Resources { get; } = new ObservableDictionary<string, int>();
    private UnitController _unitController = new();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Resources.Add("Gold", 10);
        _unitController.PlaceUnit(unitAtlas.Miner, this, Vector2.zero);
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

    private void SelectBlock(Vector2 position)
    {
        Collider2D collider = Physics2D.OverlapPoint(position);
        if (collider)
        {
            GameObject selectedBlock = collider.gameObject;
            if (selectedBlock.GetComponent<Block>() != null)
            {
                _unitController.CommandUnit(selectedBlock);
            }
        }
    }
}
