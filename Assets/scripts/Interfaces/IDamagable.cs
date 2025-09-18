public interface IDamagable
{
    public void TakeDamage(int amount, Player Damager, UnitTypeName unitType);
    public int CurrentHealth { get; }
    public int X {  get; }
    public int Y {  get; }
}
