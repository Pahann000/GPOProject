using UnityEngine;

public class GreenHouse : ProductionBuilding
{
    protected override void Start()
    {
        base.Start();

        // Инициализация ресурсов (если нужно переопределить значения)
        inputResources = new ResourcePair[] { new ResourcePair(ResourceType.Rock, 1) };
        outputResources = new ResourcePair[] { new ResourcePair(ResourceType.Minerals, 3) };
    }

    // Дополнительная специфичная логика для теплицы
    protected override void TryProduceResources()
    {
        // Проверка дня/ночи или других условий
        if (IsDayTime())
        {
            base.TryProduceResources();
        }
    }

    private bool IsDayTime()
    {
        // Ваша логика определения времени суток
        return true;
    }
}