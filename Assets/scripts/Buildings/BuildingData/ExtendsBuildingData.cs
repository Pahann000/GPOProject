using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Тип расширения, которое даёт здание.
/// </summary>
public enum ExtendType
{
	PopulationLimit,      // Увеличение лимита населения
	ResourceStorage,      // Увеличение хранилища ресурса
	ResearchUnlock,       // Разблокировка исследований (лаборатория)
	DefenseBonus,         // Бонус к обороне (например, радиус башни)
						  // другие
}

/// <summary>
/// Данные для зданий, расширяющих возможности колонии.
/// </summary>
[CreateAssetMenu(menuName = "Buildings/Extends Building")]
public class ExtendsBuildingData : BaseBuildingData
{
	[Header("Расширение")]
	public ExtendType ExtendType;
	public int BonusValue;                          // Величина бонуса (например, +10 к лимиту)
	public ResourceType StoredResource;              // Для ResourceStorage – какой ресурс
	public List<ResearchProject> AvailableProjects;  // Для лаборатории – доступные проекты
}