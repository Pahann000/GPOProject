using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private int attackRange = 1;
    [SerializeField] private int damagePerHit = 1;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject selectionIndicator;

    private Vector2 targetPosition;
    private Transform targetBlock;
    private bool isSelected = false;
    private bool isMoving = false;
    private bool isAttacking = false;
    private float lastAttackTime;

    private void Update()
    {
        if (isMoving)
        {
            MoveToPosition();
        }

        if (targetBlock != null && !isMoving)
        {
            AttackBlock();
        }
    }

    public void Select()
    {
        isSelected = true;
        selectionIndicator.SetActive(true);
    }

    public void Deselect()
    {
        isSelected = false;
        selectionIndicator.SetActive(false);
    }

    public void SetMoveTarget(Vector2 position)
    {
        targetPosition = position;
        targetBlock = null;
        isMoving = true;
        isAttacking = false;

        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
        }
    }

    public void SetAttackTarget(Transform blockTransform)
    {
        targetBlock = blockTransform;
        isMoving = false;
        isAttacking = true;
    }

    private void MoveToPosition()
    {
        //transform.position = Vector2.MoveTowards; 
    }

    private void AttackBlock()
    {
        if (targetBlock == null) return;

        // Проверка расстояния до блока
        if (Vector2.Distance(transform.position, targetBlock.position) > attackRange)
        {
            // Если слишком далеко - подойти ближе
            SetMoveTarget(targetBlock.position);
            return;
        }

        // Проверка кулдауна атаки
        if (Time.time - lastAttackTime < attackCooldown) return;

        // Поворот к цели
        if (targetBlock.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // Анимация атаки
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Нанесение урона блоку
        Block block = targetBlock.GetComponent<Block>();
        if (block.CurrentHealth != 0)
        {
            block.TakeDamage(damagePerHit);
        }

        lastAttackTime = Time.time;
    }
}