using UnityEngine;

/// <summary>
/// �����, �������������� ������������� �������� �� �����.
/// ��������� �������� ������� �� ������� ���������.
/// </summary>
public class ResourceDeposit : MonoBehaviour
{
	/// <summary>
	/// ��� �������, ������������� � �������������.
	/// </summary>
	public ResourceType resourceType;

	/// <summary>
	/// ����� ��������� ���������� �������� � �������������.
	/// </summary>
	public int totalAmount;

	/// <summary>
	/// ������� ���������� ���������� ��������.
	/// </summary>
	public int remainingAmount;

	/// <summary>
	/// ����, ����������� �� ��������� �������������.
	/// </summary>
	/// <value>True, ���� ������� ��������� �������� (remainingAmount ? 0)</value>
	public bool IsDepleted => remainingAmount <= 0;

	/// <summary>
	/// ���������� ��� ������� �������������.
	/// </summary>
	/// <returns>��� ������� (ResourceType)</returns>
	public ResourceType GetResourceType() => resourceType;

	/// <summary>
	/// ��������� ��������� ���������� �������� �� �������������.
	/// </summary>
	/// <param name="amount">������������� ���������� ��������</param>
	/// <returns>���������� ����������� ���������� ��������</returns>
	/// <remarks>
	/// ���� ������������� ���������� ��������� ����������, ���������� ������ ��������� ����������.
	/// ������������� ��������� remainingAmount �� ����������� ����������.
	/// </remarks>
	public int ExtractResource(int amount)
	{
		int extracted = Mathf.Min(amount, remainingAmount);
		remainingAmount -= extracted;
		return extracted;
	}
}