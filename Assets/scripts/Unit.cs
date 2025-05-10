using UnityEngine;
using Pathfinding;

public class Unit : MonoBehaviour
{
    [Header("Pathfinding")]
    public Transform target;
    public float activateDistance = 500f;
    public float pathUpdateSeconds = 0.5f;

    [Header("Physics")]
    public float speed = 1500f;
    public float nextWaypointDistance = 3f;
    //public float jumpNodeHeightRequirement = 0.8f;
    public float jumpForce = 100f;
    public float jumpCheckOffset = 0.6f;
    public float maxSpeed = 5f;
    public float linearDrag = 1.5f;

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
    private float _coyoteTime = 1f;
    private float _lastGroundedTime;

    public void Start()
    {
        _seeker = GetComponent<Seeker>();
        _rb = GetComponent<Rigidbody2D>();
        _rb.linearDamping = linearDrag;

        InvokeRepeating("UpdatePath", 0f, pathUpdateSeconds);
    }

    private void FixedUpdate()
    {
        if (TargetInDistance() && followEnabled)
        {
            PathFollow();
        }

        if (_isGrounded) 
        { 
            _lastGroundedTime = Time.time; 
        }
    }

    private void Update()
    {
        if (followEnabled && TargetInDistance() && _seeker.IsDone())
        {
            _seeker.StartPath(_rb.position, target.position, OnPathComplete);
        }
    }

    private bool IsGrounded()
    {
        Collider2D col = GetComponent<Collider2D>();
        float radius = col.bounds.extents.x * 0.9f; // 90% от ширины коллайдера
        RaycastHit2D hit = Physics2D.CircleCast(
            col.bounds.center,
            radius,
            Vector2.down,
            jumpCheckOffset,
            LayerMask.GetMask("Terrain")
        );
        return hit.collider != null;
    }

    bool CanJump()
    {
        return _isGrounded || Time.time - _lastGroundedTime <= _coyoteTime;
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

        // Проверка, стоит ли юнит на земле
        _isGrounded = IsGrounded();

        // высчитываем перемещение
        Vector2 direction = ((Vector2)path.vectorPath[_currentWaypoint] - _rb.position).normalized;
        Vector2 force = direction * speed * Time.deltaTime;

        // прыжок
        if (jumpEnabled && CanJump())
        {
            _rb.AddForce(Vector2.up * jumpForce);
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
            if (_rb.linearVelocity.x > 0.05f)
            {
                transform.localScale = new Vector3(-1f * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
            else if (_rb.linearVelocity.x < -0.05f)
            {
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
        }

        float maxSpeed = 5f; // Максимальная скорость
        if (_rb.linearVelocity.magnitude > maxSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private bool TargetInDistance()
    {
        return Vector2.Distance(transform.position, target.transform.position) < activateDistance;
    }

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            _currentWaypoint = 0;
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