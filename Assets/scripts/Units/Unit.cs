using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Settings")]
    /// <summary>
    /// ��� �����.
    /// </summary>
    public UnitType unitType;

    //[Header("References")]
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

    //TODO: ����� ���� - navmesh, ������� ��������
    /// <summary>
    /// �������� ����������� �����.
    /// </summary>
    /// <param name="position"></param>
    private void MoveToPosition(Vector2 position)
    {
        transform.position = new Vector2(transform.position.x - 10, transform.position.x - 10);
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
            //MoveToPosition(position);
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

    public Tile Target
    {
        get
        {
            return _target;
        }
        set
        {
            _target = value;
        }
    }
 

    public Unit(Player owner)
    {
        Owner = owner;
    }
}