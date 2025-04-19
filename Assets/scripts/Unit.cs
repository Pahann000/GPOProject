using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Settings")]
    /// <summary>
    /// Тип юнита.
    /// </summary>
    public UnitType unitType;

    //[Header("References")]
    //public Animator animator;
    //public GameObject selectionIndicator;

    /// <summary>
    /// Цель юнита.
    /// </summary>
    private GameObject _target;
    /// <summary>
    /// Время предыдущей атаки.
    /// </summary>
    private float _lastAttackTime;
    /// <summary>
    /// текущая работа юнита.
    /// </summary>
    private UnitWork _currentUnitWork;

    void FixedUpdate()
    {
        if (Target != null)
        {
            AttackBlock(Target);
        }
    }

    //public void Select()
    //{
    //    selectionIndicator.SetActive(true);
    //}

    //public void Deselect()
    //{
    //    selectionIndicator.SetActive(false);
    //}

    /// <summary>
    /// Возвращает и задаёт цель юнита.
    /// </summary>
    public GameObject Target
    {
        get
        {
            return _target;
        }
        set
        {
            if(value != null)
            {
                _target = value;
            }
        }
    }

    //TODO: поиск пути - navmesh, Муравьи Лэнгтона
    /// <summary>
    /// Алгоритм предвижения юнита.
    /// </summary>
    /// <param name="position"></param>
    private void MoveToPosition(Vector2 position)
    {
        transform.position = Vector2.right;
    }

    /// <summary>
    /// Заставляет юнита атаковать блок.
    /// </summary>
    /// <param name="target">Блок для атаки.</param>
    private void AttackBlock(GameObject target)
    {

        // Проверка расстояния до блока
        if (Vector2.Distance(transform.position, target.transform.position) > unitType.AttackRange)
        {
            // Если слишком далеко - подойти ближе
            MoveToPosition(target.transform.position);
            return;
        }

        // Проверка кулдауна атаки
        if (Time.time - _lastAttackTime < unitType.AttackCooldown) return;

        // Поворот к цели
        if (target.transform.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        //// Анимация атаки
        //if (animator != null)
        //{
        //    animator.SetTrigger("Attack");
        //}

        // Нанесение урона блоку
        Block block = target.GetComponent<Block>();
        if (block.CurrentHealth - 1 != 0)
        {
            block.TakeDamage(unitType.DamagePerHit);
        }
        else
        {
            block.TakeDamage(unitType.DamagePerHit);
            Target = null;
        }

        _lastAttackTime = Time.time;
    }
}