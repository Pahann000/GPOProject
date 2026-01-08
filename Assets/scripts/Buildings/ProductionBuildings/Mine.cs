using UnityEngine;

public class GreenHouse : ProductionBuilding
{
    protected override void Start()
    {
        base.Start();

        // »нициализаци€ ресурсов (если нужно переопределить значени€)
        inputResources = new ResourceBundle(
            new ResourceBundle.ResourcePair
            {
                Type = ResourceType.Rock,
                Amount = 1
            }
        );

        outputResources = new ResourceBundle(
            new ResourceBundle.ResourcePair
            {
                Type = ResourceType.Minerals,
                Amount = 3
            }
        );
    }

    // ƒополнительна€ специфична€ логика дл€ теплицы
    protected override void TryProduceResources()
    {
        // ѕроверка дн€/ночи или других условий
        if (IsDayTime())
        {
            base.TryProduceResources();
        }
    }

    private bool IsDayTime()
    {
        // ¬аша логика определени€ времени суток
        return true;
    }
}