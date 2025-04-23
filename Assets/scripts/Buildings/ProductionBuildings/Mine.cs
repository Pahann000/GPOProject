using UnityEngine;

/// <summary>
/// Шахта - производственное здание для добычи ресурсов из месторождений.
/// Наследует базовую логику производства от <see cref="ProductionBuilding"/>.
/// </summary>
/// <remarks>
/// Автоматически находит ближайшее доступное месторождение и добывает ресурсы с заданной эффективностью.
/// </remarks>
public class Mine : ProductionBuilding
{
    [Header("Mining Settings")]
    /// <summary>
    /// Текущее целевое месторождение для добычи.
    /// </summary>
    ResourceDeposit targetDeposit;

    /// <summary>
    /// Эффективность добычи (множитель производства).
    /// </summary>
    /// <value>
    /// Значение от 0 до 1, где 1 - 100% эффективность.
    /// Влияет на количество добываемых ресурсов.
    /// </value>
    public float miningEfficiency = 1f;

    /// <summary>
    /// Производит добычу ресурсов из месторождения.
    /// </summary>
    /// <remarks>
    /// Если месторождение не назначено или истощено, выполняет поиск нового.
    /// Добывает количество ресурсов, равное ProductionAmount * miningEfficiency.
    /// Добавляет добытые ресурсы в общий пул через ResourceManager.
    /// </remarks>
    protected override void ProduceResource()
    {
        if (targetDeposit == null || targetDeposit.IsDepleted)
        {
            FindNearestDeposit();
            return;
        }

        // Добыча ресурса с учетом эффективности
        int extractedAmount = Mathf.RoundToInt(ProductionAmount * miningEfficiency);
        ResourceType extractedResource = targetDeposit.GetResourceType();

        targetDeposit.ExtractResource(extractedAmount);
        //ResourceManager.Instance.AddResource(extractedResource, extractedAmount);
    }

    /// <summary>
    /// Находит ближайшее доступное месторождение ресурсов.
    /// </summary>
    /// <remarks>
    /// Сканирует все месторождения на сцене и выбирает ближайшее неистощенное.
    /// Если месторождений не найдено, выводит предупреждение в лог.
    /// </remarks>
    private void FindNearestDeposit()
    {
        // Поиск ближайшего месторождения
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
    /// Завершает строительство шахты и ищет ближайшее месторождение.
    /// </summary>
    /// <remarks>
    /// Вызывает базовую реализацию CompleteConstruction, затем автоматически
    /// назначает ближайшее месторождение для добычи.
    /// </remarks>
    public override void CompleteConstruction()
    {
        base.CompleteConstruction();
        FindNearestDeposit();
    }
}