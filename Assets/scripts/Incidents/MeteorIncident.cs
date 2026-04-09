using UnityEngine;

[CreateAssetMenu(fileName = "Meteor", menuName = "Incidents/Meteor")]
public class MeteorIncident : GameIncident
{
    public int Damage = 100;
    public float Radius = 3f;

    public override void Execute(GameKernel kernel)
    {
        var world = kernel.GetSystem<WorldSystem>();
        if (world == null || world.WorldMap == null) return;

        Building[] allBuildings = Object.FindObjectsByType<Building>(FindObjectsSortMode.None);
        Vector2 targetPos;

        // Чтобы было интереснее, с вероятностью 70% метеорит падает недалеко от базы игрока
        if (allBuildings.Length > 0 && Random.value < 0.7f)
        {
            Building randomBuilding = allBuildings[Random.Range(0, allBuildings.Length)];
            // Падает в случайной точке в радиусе 5 клеток от выбранного здания
            targetPos = (Vector2)randomBuilding.transform.position + Random.insideUnitCircle * 5f;
        }
        else
        {
            targetPos = new Vector2(Random.Range(10, world.WorldMap.Width - 10), Random.Range(10, world.WorldMap.Height - 10));
        }

        // физический урон
        Collider2D[] hits = Physics2D.OverlapCircleAll(targetPos, Radius);
        foreach (var hit in hits)
        {
            var damagable = hit.GetComponentInParent<IDamagable>();

            if (damagable != null)
            {
                damagable.TakeDamage(Damage, null, UnitTypeName.Worker);
            }
        }

        // кратер с ресурсами
        int mapX = Mathf.RoundToInt(targetPos.x);
        int mapY = Mathf.RoundToInt(targetPos.y);
        world.WorldMap.PlaceBlock(mapX, mapY, BlockType.Minerals);

        Debug.Log($"<color=red>[УГРОЗА]</color> Метеорит упал в точке {targetPos}! Радиус поражения: {Radius}");

        // TODO: Заспавнить эффект взрыва (Particles) в targetPos
    }
}