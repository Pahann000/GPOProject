using Pathfinding;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Unit : MonoBehaviour
{
    private Path _path;
    private int _currentWaypoint = 0;
    private float obstacleCheckDistance = 1f; // Дистанция проверки препятствий

    public Rigidbody2D rb; // Физика юнита
    public Seeker Seeker;
    public float jumpForce = 5f; // Сила прыжка
    public float speed = 2f; // Скорость передвижения
    public LayerMask obstacleLayer; // Слой препятствий
    public float nextWaypointDistance = 0.5f; // Минимальное расстояние для перехода к следующей точке

    [Header("Unit Settings")]
    /// <summary>
    /// ��� �����.
    /// </summary>
    public UnitType unitType;

    //public Animator animator;
    //public GameObject selectionIndicator;

    /// <summary>
    /// ���� �����.
    /// </summary>
    private Tile _target;
    /// <summary>
    /// ����� ���������� �����.
    /// </summary>
    private float _lastAttackTime;
    /// <summary>
    /// ������� ������ �����.
    /// </summary>
    public UnitWork CurrentUnitWork { get; private set; }
    public Player Owner;


    void FixedUpdate()
    {
        if (Target != null)
        {
            AttackBlock(Target);
        }
    }

    private bool IsGrounded(Vector2 point)
    {
        RaycastHit2D hit = Physics2D.Raycast(point, Vector2.down, 1f, obstacleLayer);
        return hit.collider != null;
    }

    //TODO: ����� ���� - navmesh, ������� ��������
    /// <summary>
    /// �������� ����������� �����.
    /// </summary>
    /// <param name="position"></param>
    private void MoveToPosition(Vector2 position)
    {
        if (_path == null || _currentWaypoint >= _path.vectorPath.Count) return;

        Vector2 targetPos = (Vector2)_path.vectorPath[_currentWaypoint];

        // Проверяем, находится ли точка пути на земле
        if (!IsGrounded(targetPos))
        {
            Debug.Log("Точка пути недоступна, пропускаем её!");
            _currentWaypoint++;
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
            _currentWaypoint++;
        }
    }

    
    /// <summary>
    /// ���������� ����� ��������� ����.
    /// </summary>
    /// <param name="target">���� ��� �����.</param>
    private void AttackBlock(Tile target)
    {
        if (target == null) { return; }
        Vector2 position = new Vector2(target.x, target.y);

        // �������� ���������� �� �����
        if (Vector2.Distance(transform.position, position) > unitType.AttackRange)
        {
            // ���� ������� ������ - ������� �����
            MoveToPosition(position);
            return;
        }

        // �������� �������� �����
        if (Time.time - _lastAttackTime < unitType.AttackCooldown) return;

        // ������� � ����
        if (target.x > transform.position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        //// �������� �����
        //if (animator != null)
        //{
        //    animator.SetTrigger("Attack");
        //}
        if (target.CurrentHealth - unitType.DamagePerHit > 0)
        {
            target.TakeDamage(unitType.DamagePerHit, Owner);
        }
        else
        {
            target.TakeDamage(unitType.DamagePerHit, Owner);
            Target = null;
        }

        _lastAttackTime = Time.time;
    }

    //public void Select()
    //{
    //    selectionIndicator.SetActive(true);
    //}

    //public void Deselect()
    //{
    //    selectionIndicator.SetActive(false);
    //}

    private void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            _path = p;
            _currentWaypoint = 0;
        }
    }

    public Tile Target
    {
        get
        {
            return _target;
        }
        set
        {
            _target = value;
            if (_target != null)
            {
                Vector3 targetPos = new Vector3(_target.x, _target.y, 0);
                Seeker.StartPath(transform.position, targetPos, OnPathComplete);
            }
        }
    }
}