using UnityEngine;

/// <summary>
/// Конкретная турель, наследующая DefenseBuilding.
/// </summary>
public class Turret : DefenseBuilding
{
    [SerializeField] private Transform turretHead; // для поворота

    protected override GameObject FindTarget()
    {
        // Простейший поиск: физический оверлап сфера
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, DefenseData.Range, DefenseData.TargetMask);
        if (hits.Length > 0)
        {
            // Берём первого попавшегося (можно улучшить: ближайший, с наименьшим здоровьем и т.д.)
            return hits[0].gameObject;
        }
        return null;
    }

    protected override void Attack(GameObject target)
    {
        // Поворачиваем башню
        if (turretHead != null)
        {
            Vector3 direction = target.transform.position - turretHead.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            turretHead.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        // Наносим урон
        IDamagable damageable = target.GetComponent<IDamagable>();
        //if (damageable != null)
        //{
        //    // Передаём урон; Player и UnitType могут быть известны из контекста (например, владелец)
        //    damageable.TakeDamage(DefenseData.Damage, GetComponent<Player>(), UnitTypeName.Turret);
        //}

        // Эффекты выстрела
        if (DefenseData.ShootEffect != null)
        {
            DefenseData.ShootEffect.Play();
        }
        if (DefenseData.ProjectilePrefab != null)
        {
            Instantiate(DefenseData.ProjectilePrefab, turretHead.position, turretHead.rotation);
        }
        if (DefenseData.ShootSound != null)
        {
            AudioSource.PlayClipAtPoint(DefenseData.ShootSound, transform.position);
        }
    }
}