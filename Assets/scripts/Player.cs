using UnityEngine;

public class Player : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
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
            Block selectedBlock = collider.GetComponentInParent<Block>();
            Debug.Log($"блок найден:{selectedBlock.BlockType.Name}");
            if (selectedBlock != null)
            {
                SetDestoroyBlock(selectedBlock);
            }
        }
    }

    public void SetDestoroyBlock(Block blockToDestroy)
    {
        blockToDestroy.TakeDamage(1);
    }
}
