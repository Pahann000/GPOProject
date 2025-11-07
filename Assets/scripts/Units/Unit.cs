using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, IDamagable, IChunkObserver
{
    private float _speed = 2f;
    //private float _jumpForce = 8f;
    private IDamagable _target;
    private Coroutine _currentAction;

    public Rigidbody2D rb;
    public UnitType unitType;
    public Player Owner;

    public int X => (int)transform.position.x;

    public int Y => (int)transform.position.y;

    public int CurrentHealth { get; set; }

    public UnitWork CurrentUnitWork { get; private set; }

    public IDamagable Target
    {
        get => _target;
        set => SetTarget(value);
    }

    private void Start() => ChunkManager.Instance.RegisterObserver(this);
    private void OnDestroy() => ChunkManager.Instance.UnregisterObserver(this);

    /// <summary>
    /// Начать последовательность: движение -> атака
    /// </summary>
    private void SetTarget(IDamagable target)
    {
        if (_currentAction != null)
        {
            StopCoroutine(_currentAction);
        }

        _target = target;
        _currentAction = StartCoroutine(Attack());
    }

    private IEnumerator Attack()
    {
        while (_target != null && _target.CurrentHealth > 0)
        {
            yield return StartCoroutine(PerformAttack());
            yield return new WaitForSeconds(unitType.AttackCooldown);
        }
    }

    private IEnumerator MoveToTarget()
    {
        CurrentUnitWork = UnitWork.Moving;

        Vector2Int targetPos = new Vector2Int(_target.X, _target.Y);
        List<Vector2Int> path = Pathfinding.FindPath(
            new Vector2Int(X, Y),
            targetPos
        );

        if (path == null)
        {
            CurrentUnitWork = UnitWork.Idle;
            _target = null;
            yield break;
        }
        foreach (Vector2 point in path)
        {
            Vector3 direction = Vector2.right;
            if (point.x <= X)
            {
                direction = Vector2.left;
            }

            if(point.y > Y)
            {
                transform.Translate(_speed * Time.deltaTime * direction);
            }

            while (Vector3.Distance(transform.position, point) > 1.5f)
            {
                transform.Translate(_speed * Time.deltaTime * direction);

                yield return null;
            }
        }
    }

    private IEnumerator PerformAttack()
    {
        CurrentUnitWork = UnitWork.Attacking;

        if (Vector2.Distance(transform.position, new Vector2(_target.X, _target.Y)) > unitType.AttackRange)
        {
            yield return StartCoroutine(MoveToTarget());
        }
        else
        {
            _target.TakeDamage(unitType.DamagePerHit, Owner, unitType.UnitTypeName);
            Debug.Log(_target.CurrentHealth);
            if (_target.CurrentHealth <= 0)
            {
                _target = null;
                CurrentUnitWork = UnitWork.Idle;
            }
        }
    }

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
}