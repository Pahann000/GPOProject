using UnityEngine;

/// <summary>
/// ����� - ���������������� ������ ��� ������ �������� �� �������������.
/// ��������� ������� ������ ������������ �� <see cref="ProductionBuilding"/>.
/// </summary>
/// <remarks>
/// ������������� ������� ��������� ��������� ������������� � �������� ������� � �������� ��������������.
/// </remarks>
public class Mine : ProductionBuilding
{
    [Header("Mining Settings")]
    /// <summary>
    /// ������� ������� ������������� ��� ������.
    /// </summary>
    ResourceDeposit targetDeposit;

    /// <summary>
    /// ������������� ������ (��������� ������������).
    /// </summary>
    /// <value>
    /// �������� �� 0 �� 1, ��� 1 - 100% �������������.
    /// ������ �� ���������� ���������� ��������.
    /// </value>
    public float miningEfficiency = 1f;

    /// <summary>
    /// ���������� ������ �������� �� �������������.
    /// </summary>
    /// <remarks>
    /// ���� ������������� �� ��������� ��� ��������, ��������� ����� ������.
    /// �������� ���������� ��������, ������ ProductionAmount * miningEfficiency.
    /// ��������� ������� ������� � ����� ��� ����� ResourceManager.
    /// </remarks>
    protected override void ProduceResource()
    {
        if (targetDeposit == null || targetDeposit.IsDepleted)
        {
            FindNearestDeposit();
            return;
        }

        // ������ ������� � ������ �������������
        int extractedAmount = Mathf.RoundToInt(ProductionAmount * miningEfficiency);
        ResourceType extractedResource = targetDeposit.GetResourceType();

        targetDeposit.ExtractResource(extractedAmount);
        //ResourceManager.Instance.AddResource(extractedResource, extractedAmount);
    }

    /// <summary>
    /// ������� ��������� ��������� ������������� ��������.
    /// </summary>
    /// <remarks>
    /// ��������� ��� ������������� �� ����� � �������� ��������� ������������.
    /// ���� ������������� �� �������, ������� �������������� � ���.
    /// </remarks>
    private void FindNearestDeposit()
    {
        // ����� ���������� �������������
        ResourceDeposit[] deposits = FindObjectsOfType<ResourceDeposit>();
        float minDistance = float.MaxValue;

        foreach (var deposit in deposits)
        {
            if (deposit.IsDepleted) continue;

            float distance = Vector3.Distance(transform.position, deposit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                targetDeposit = deposit;
            }
        }

        if (targetDeposit == null)
        {
            Debug.LogWarning("No available resource deposits found!");
        }
    }

    /// <summary>
    /// ��������� ������������� ����� � ���� ��������� �������������.
    /// </summary>
    /// <remarks>
    /// �������� ������� ���������� CompleteConstruction, ����� �������������
    /// ��������� ��������� ������������� ��� ������.
    /// </remarks>
    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        FindNearestDeposit();
    }
}