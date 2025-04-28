using UnityEngine;

[CreateAssetMenu(fileName = "UnitType", menuName = "Unit type")]
public class UnitType : ScriptableObject
{
    public float MoveSpeed;
    public float AttackCooldown;
    public int AttackRange;
    public int DamagePerHit;
    public UnitTypeName UnitTypeName;
    public Sprite Sprite;
}
