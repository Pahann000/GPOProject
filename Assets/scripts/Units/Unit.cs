using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс юнита, управляемого игроком.
/// </summary>
public class Unit : MonoBehaviour, IDamagable, IChunkObserver
{
    private float _speed = 2f;
    private float _jumpForce = 4f;
    private bool _isGrounded = true;
    private IDamagable _target;
    private Coroutine _currentAction;

    public Rigidbody2D rb;
    public UnitType unitType;
    public Player Owner;

    /// <summary>
    /// Положение юнита по X.
    /// </summary>
    public int X => (int)transform.position.x;

    /// <summary>
    /// Положение юнита по Y.
    /// </summary>
    public int Y => (int)transform.position.y;

    /// <summary>
    /// Текущее здоровье юнита.
    /// </summary>
    public int CurrentHealth { get; set; }

    /// <summary>
    /// Текущая работа юнита.
    /// </summary>
    public UnitWork CurrentUnitWork { get; private set; }

    /// <summary>
    /// Текущая цель атаки юнита.
    /// </summary>
    public IDamagable Target
    {
        get => _target;
        set => SetTarget(value);
    }

    private void Start()
    {
        ChunkManager.Instance.RegisterObserver(this);
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        // РЕГИСТРАЦИЯ В СИСТЕМЕ
        if (GameKernel.Instance != null)
        {
            var unitSys = GameKernel.Instance.GetSystem<UnitSystem>();
            unitSys?.RegisterUnit(this);
        }
    }

    private void OnDestroy()
    {
        ChunkManager.Instance.UnregisterObserver(this);

        // ОТПИСКА
        if (GameKernel.Instance != null)
        {
            var unitSys = GameKernel.Instance.GetSystem<UnitSystem>();
            unitSys?.UnregisterUnit(this);
        }
    }

    private void Update()
    {
        if (GameKernel.Instance != null)
        {
            var unitSys = GameKernel.Instance.GetSystem<UnitSystem>();
            if (unitSys != null && !unitSys.IsActive) return;
        }

        // Тут могла бы быть логика, если бы она не была в корутинах
    }

    private void FixedUpdate()
    {
        // Простая проверка через raycast вниз (длина 0.1f, слой "Ground" — настройте под вашу игру)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 0.1f, LayerMask.GetMask("Default"));
        _isGrounded = hit.collider != null;
    }

    public void Jump()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, _jumpForce);
            _isGrounded = false;
        }
    }

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
        foreach (Vector2Int point in path)
        {
            Vector3 direction = Vector2.right;
            if (point.x <= X)
            {
                direction = Vector2.left;
            }

            while (Vector3.Distance(transform.position, new Vector3(point.x, point.y, 0)) > 1.5f)
            {
                transform.Translate(_speed * Time.deltaTime * direction);
                if (point.y > Y + 0.5f && _isGrounded)
                {
                    Jump();
                }

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

    /// <summary>
    /// Наносит урон юниту.
    /// </summary>
    /// <param name="amount"> Количество урона </param>
    /// <param name="Damager"> Игрок, нанёсший урон. </param>
    /// <param name="unitType"> Тип юнита, нанёсшего урон. </param>
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