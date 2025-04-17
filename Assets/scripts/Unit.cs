using UnityEngine;

public class Unit : MonoBehaviour
{
    [Header("Unit Settings")]
    public UnitType unitType;

    //[Header("References")]
    //public Animator animator;
    //public GameObject selectionIndicator;

    private GameObject _target;
    private float _lastAttackTime;
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

    //TODO: ����� ���� - navmesh, ������� ��������
    private void MoveToPosition(Vector2 position)
    {
        transform.position = Vector2.right;
    }

    private void AttackBlock(GameObject target)
    {

        // �������� ���������� �� �����
        if (Vector2.Distance(transform.position, target.transform.position) > unitType.AttackRange)
        {
            // ���� ������� ������ - ������� �����
            MoveToPosition(target.transform.position);
            return;
        }

        // �������� �������� �����
        if (Time.time - _lastAttackTime < unitType.AttackCooldown) return;

        // ������� � ����
        if (target.transform.position.x > transform.position.x)
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

        // ��������� ����� �����
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