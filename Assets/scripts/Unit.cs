using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Settings")]
    public UnitType unitType;
    private GameObject _target;
    private Vector2 _currentDirection;
    private float _lastAttackTime;

    void FixedUpdate()
    {
        //if (_target == null) return;

        //MoveToTarget();
        //TryAttack();
    }

    private void MoveToTarget()
    {
        Vector2 direction = (_target.transform.position - transform.position).normalized;
        transform.position += (Vector3)direction * unitType.MoveSpeed * Time.fixedDeltaTime;

        // Поворот спрайта
        transform.localScale = new Vector3(
            direction.x > 0 ? 1 : -1,
            1,
            1
        );
    }

    private void TryAttack()
    {
        if (Vector2.Distance(transform.position, _target.transform.position) > unitType.AttackRange)
            return;

        if (Time.time - _lastAttackTime < unitType.AttackCooldown)
            return;

        if (_target.TryGetComponent<Block>(out var block))
        {
            block.TakeDamage(unitType.DamagePerHit);
            _lastAttackTime = Time.time;

            if (block.CurrentHealth <= 0)
                _target = null;
        }
    }

    public void SetTarget(GameObject target)
    {
        _target = target;
    }

    [Header("Selection")]
    public GameObject selectionIndicator; // Префаб или дочерний объект

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