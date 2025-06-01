using UnityEngine;

public class GreenHouse : ProductionBuilding
{
    protected override void Start()
    {
        base.Start();

        // ������������� �������� (���� ����� �������������� ��������)
        inputResources = new ResourceBundle(
            new ResourceBundle.ResourcePair
            {
                Type = ResourceType.Water,
                Amount = 1
            }
        );

        outputResources = new ResourceBundle(
            new ResourceBundle.ResourcePair
            {
                Type = ResourceType.Food,
                Amount = 3
            }
        );
    }

    // �������������� ����������� ������ ��� �������
    protected override void TryProduceResources()
    {
        // �������� ���/���� ��� ������ �������
        if (IsDayTime())
        {
            base.TryProduceResources();
        }
    }

    private bool IsDayTime()
    {
        // ���� ������ ����������� ������� �����
        return true;
    }
}