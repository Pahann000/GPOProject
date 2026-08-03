using UnityEngine;


[CreateAssetMenu(menuName = "Buildings/Produce Building")]
public class ProduceBuildingData : BaseBuildingData
{
	[Header("Производство")]
	public ResourceBundle InputResources;       // Затраты за цикл
	public ResourceBundle OutputResources;      // Результат за цикл
	public float ProductionInterval = 5f;       // Длительность цикла в секундах

	[Header("Спавн юнитов (если применимо)")]
	public bool SpawnsUnits;                    // true = создаёт юнитов
	public GameObject UnitPrefab;               // Префаб юнита (если спавнит)
	public int UnitsPerCycle = 1;                // Количество юнитов за цикл
	public Transform SpawnPoint;                 // Точка появления (опционально, относительно здания)
}