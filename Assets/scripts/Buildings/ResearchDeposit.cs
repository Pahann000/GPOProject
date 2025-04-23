using UnityEngine;

/// <summary>
///  ласс, представл€ющий месторождение ресурсов на карте.
/// ѕозвол€ет добывать ресурсы до полного истощени€.
/// </summary>
public class ResourceDeposit : MonoBehaviour
{
	/// <summary>
	/// “ип ресурса, содержащегос€ в месторождении.
	/// </summary>
	public ResourceType resourceType;

	/// <summary>
	/// ќбщее начальное количество ресурсов в месторождении.
	/// </summary>
	public int totalAmount;

	/// <summary>
	/// “екущее оставшеес€ количество ресурсов.
	/// </summary>
	public int remainingAmount;

	/// <summary>
	/// ‘лаг, указывающий на истощение месторождени€.
	/// </summary>
	/// <value>True, если ресурсы полностью истощены (remainingAmount ? 0)</value>
	public bool IsDepleted => remainingAmount <= 0;

	/// <summary>
	/// ¬озвращает тип ресурса месторождени€.
	/// </summary>
	/// <returns>“ип ресурса (ResourceType)</returns>
	public ResourceType GetResourceType() => resourceType;

	/// <summary>
	/// »звлекает указанное количество ресурсов из месторождени€.
	/// </summary>
	/// <param name="amount">«апрашиваемое количество ресурсов</param>
	/// <returns>‘актически извлеченное количество ресурсов</returns>
	/// <remarks>
	/// ≈сли запрашиваемое количество превышает оставшеес€, возвращает только доступное количество.
	/// јвтоматически уменьшает remainingAmount на извлеченное количество.
	/// </remarks>
	public int ExtractResource(int amount)
	{
		int extracted = Mathf.Min(amount, remainingAmount);
		remainingAmount -= extracted;
		return extracted;
	}
}