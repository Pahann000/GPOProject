using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, IDamagable
{
    private List<Vector2Int> currentPath;
    private int pathIndex;
    private float _jumpForce = 20f; // Сила прыжка
    private float _speed = 2f; // Скорость передвижения

    public Rigidbody2D rb; // Физика юнита

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
    private IDamagable _target;
    /// <summary>
    /// ����� ���������� �����.
    /// </summary>
    private float _lastAttackTime;
    /// <summary>
    /// ������� ������ �����.
    /// </summary>
    public Player Owner;

    public UnitWork CurrentUnitWork { get; private set; }

    public Vector2Int CurrentGridPosition
    {
        get
        {
            return new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
        }
    }

    public bool CanReachPosition(Vector2Int targetGridPosition)
    {
        return Pathfinding.FindPath(CurrentGridPosition, targetGridPosition) != null;
    }

    public void MoveTo(Vector2Int targetPosition)
    {
        Vector2Int startPosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );

        currentPath = Pathfinding.FindPath(startPosition, targetPosition);
        pathIndex = 0;

        if (currentPath != null)
            FollowPath();
    }

    private IEnumerator FollowPath()
    {
        while (pathIndex < currentPath.Count)
        {
            Vector3 targetWorldPos = new Vector3(
                currentPath[pathIndex].x,
                currentPath[pathIndex].y,
                0
            );

            while (transform.position != targetWorldPos)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetWorldPos,
                    _speed * Time.deltaTime
                );
                yield return null;
            }

            pathIndex++;
        }
    }

    /// <summary>
    /// ���������� ����� ��������� ����.
    /// </summary>
    /// <param name="target">���� ��� �����.</param>
    private void AtackTarget(IDamagable target)
    {
        Vector2Int targetPos = new Vector2Int(
        Mathf.RoundToInt(target.X),
        Mathf.RoundToInt(target.Y)
        );

        if (target == null || CanReachPosition(targetPos)) { return; }

        // �������� ���������� �� �����
        if (Vector2.Distance(transform.position, targetPos) > unitType.AttackRange)
        {
            // ���� ������� ������ - ������� �����
            MoveTo(targetPos);
        }

        // �������� �������� �����
        if (Time.time - _lastAttackTime < unitType.AttackCooldown) return;

        // ������� � ����
        if (target.X > transform.position.x)
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
            target.TakeDamage(unitType.DamagePerHit, Owner, unitType.UnitTypeName);
        }
        else
        {
            target.TakeDamage(unitType.DamagePerHit, Owner, unitType.UnitTypeName);
            Target = null;
        }

        Debug.Log(target.CurrentHealth);

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

    public void TakeDamage(int amount, Player Damager, UnitTypeName unitType)
    {
        if (unitType == UnitTypeName.Soldier)
        {
            CurrentHealth -= amount * 2;
        }
        else
        {
            CurrentHealth -= amount / 2;
        }

        if (CurrentHealth <= 0)
        {
            CurrentUnitWork = UnitWork.Dead;
        }
    }

    public IDamagable Target
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
                AtackTarget(_target);
            }
        }
    }

    public int X => (int)transform.position.x;
    public int Y => (int)transform.position.y;

    public int CurrentHealth { get; set; }

}