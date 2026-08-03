using UnityEngine;

/// <summary>
/// Данные для оборонительных сооружений (турели, ловушки).
/// </summary>
[CreateAssetMenu(menuName = "Buildings/Defense Building")]
public class DefenseBuildingData : BaseBuildingData
{
    [Header("Боевые характеристики")]
    public float Range = 5f;
    public int Damage = 10;
    public float Cooldown = 1f;
    public LayerMask TargetMask;                // Какие слои атаковать

    [Header("Тип")]
    public bool IsOneTimeTrap = false;          // Одноразовая ловушка

    [Header("Визуальные эффекты")]
    public GameObject ProjectilePrefab;          // Снаряд (если есть)
    public ParticleSystem ShootEffect;
    public AudioClip ShootSound;
}