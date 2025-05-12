using UnityEngine;

public class Player : MonoBehaviour
{
    public UnitAtlas unitAtlas;
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
        Debug.Log($"нажатие сделано на позиции{position}");
        if (collider)
        {
            GameObject selectedBlock = collider.gameObject;
            if (selectedBlock.GetComponent<Block>() != null)
            {
                Debug.Log($"блок найден:{selectedBlock.GetComponent<Block>().BlockType.Name}");
                _unitController.CommandUnit(selectedBlock);
            }
        }
    }
}
