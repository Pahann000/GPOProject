using UnityEngine;
using Pathfinding;

public class Unit : MonoBehaviour
{
    [Header("Pathfinding")]
    public Transform target;
    public float activateDistance = 1f;
    public float pathUpdateSeconds = 0.5f;

    [Header("Physics")]
    public float speed = 5f;
    public float nextWaypointDistance = 1f;
    public float jumpNodeHeightRequirement = 1f;
    public float jumpModifier = 2f;
    public float jumpCheckOffset = 0.4f;

    [Header("Custom Behavior")]
    public bool followEnabled = true;
    public bool jumpEnabled = true;
    public bool directionLookEnabled = true;


    [Header("Selection")]
    public GameObject selectionIndicator;

    public void Select()
    {
        // Визуальное выделение
        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        // Можно добавить другие эффекты (изменение цвета и т.д.)
        GetComponent<SpriteRenderer>().color = Color.yellow;
    }

    public void Deselect()
    {
        // Снятие выделения
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        GetComponent<SpriteRenderer>().color = Color.white;
    }
}