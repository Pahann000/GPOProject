using Pathfinding;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public Transform target;
    public UnitType unitType;
    private Seeker seeker;
    private Path path;
    private int currentWaypoint = 0;
    public float jumpForce = 5f; // Сила прыжка
    public float speed = 2f; // Скорость передвижения
    public float obstacleCheckDistance = 1f; // Дистанция проверки препятствий
    public LayerMask obstacleLayer; // Слой препятствий
    private Rigidbody2D rb; // Физика юнита
    public float nextWaypointDistance = 0.5f; // Минимальное расстояние для перехода к следующей точке

    void Start()
    {
        obstacleLayer = LayerMask.GetMask("Terrain");
        InitializeComponents();
    }

    void InitializeComponents()
    {
        GameObject unitsParent = GameObject.Find("Units");
        if (unitsParent == null)
        {
            unitsParent = new GameObject("Units");
        }

        unitsParent.transform.position = new Vector3(5f, 350f, 0);

        gameObject.transform.parent = unitsParent.transform;

        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = unitType.Sprite;
        renderer.sortingLayerName = "Units";
        renderer.sortingOrder = 1;

        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.linearDamping = 1.5f;
        rb.freezeRotation = true;

        seeker = gameObject.AddComponent<Seeker>();
    }

    private bool IsGrounded(Vector2 point)
    {
        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.down, 1f, obstacleLayer);
        return hit.collider != null;
    }

    void Update()
    {
        if (path == null || currentWaypoint >= path.vectorPath.Count) return;

        // Проверяем, достиг ли юнит блока (расстояние <= 1)
        if (Vector2.Distance(transform.position, target.position) <= 2f)
        {
            Debug.Log("Юнит достиг цели и останавливается!");
            path = null; // Очищаем путь, чтобы прекратить движение
            return;
        }

        Vector2 targetPos = (Vector2)path.vectorPath[currentWaypoint];

        // Проверяем, находится ли точка пути на земле
        if (!IsGrounded(targetPos))
        {
            Debug.Log("Точка пути недоступна, пропускаем её!");
            currentWaypoint++;
            return;
        }

        Vector2 direction = (targetPos - (Vector2)transform.position);

        if (Physics2D.Raycast(transform.position, direction, obstacleCheckDistance, obstacleLayer))
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
        else
        {
            transform.position += (Vector3)direction * Time.deltaTime * speed;
        }

        if (Vector2.Distance(transform.position, targetPos) < nextWaypointDistance)
        {
            currentWaypoint++;
        }
    }


    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        seeker.StartPath(transform.position, target.position, OnPathComplete);
    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }
}
