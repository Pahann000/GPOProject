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

    private Path path;
    private int _currentWaypoint = 0;
    private bool _isGrounded = false;
    private Seeker _seeker;
    private Rigidbody2D _rb;

    public void Start()
    {
        _seeker = GetComponent<Seeker>();
        _rb = GetComponent<Rigidbody2D>();

        InvokeRepeating("UpdatePath", 0f, pathUpdateSeconds);
    }

    private void FixedUpdate()
    {
        if (TargetInDistance() && followEnabled)
        {
            PathFollow();
        }
    }

    private void Update()
    {
        if (followEnabled && TargetInDistance() && _seeker.IsDone())
        {
            _seeker.StartPath(_rb.position, target.position, OnPathComplete);
        }
    }

    /// <summary>
    /// Метод отвечает за движение по пути
    /// </summary>
    private void PathFollow()
    {
        // нет пути
        if (path == null)
        {
            return;
        }

        // конец пути
        if (_currentWaypoint >= path.vectorPath.Count)
        {
            return;
        }

        // проверка касаемся ли чего-то, то есть стоим ли
        _isGrounded = Physics2D.Raycast(transform.position, -Vector3.up, GetComponent<Collider2D>().bounds.extents.y + jumpCheckOffset);

        // высчитываем перемещение
        Vector2 direction = ((Vector2)path.vectorPath[_currentWaypoint] - _rb.position).normalized;
        Vector2 force = direction * speed * Time.deltaTime;

        // прыжок
        if (jumpEnabled && _isGrounded)
        {
            _rb.AddForce(Vector2.up * speed * jumpModifier);
        }

        // передвижение
        _rb.AddForce(force);

        // след. точка
        float distance = Vector2.Distance(_rb.position, path.vectorPath[_currentWaypoint]);
        if (distance < nextWaypointDistance)
        {
            _currentWaypoint++;
        }

        // направление
        if (directionLookEnabled)
        {
            if (_rb.velocity.x > 0.05f)
            {
                transform.localScale = new Vector3(-1f * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (_rb.velocity.x < -0.05f)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }
    }

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