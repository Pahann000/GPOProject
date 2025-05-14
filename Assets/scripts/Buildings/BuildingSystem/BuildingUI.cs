using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ���������������� ��������� ��� ������ � ������������� ������
/// </summary>
public class BuildingUI : MonoBehaviour
{
    [Header("Building Settings")]
    [SerializeField] private BuildingData[] _availableBuildings; // ��������� ��� ������������� ������
    [SerializeField] private Button _buildingButtonPrefab;      // ������ ������ ������
    [SerializeField] private Transform _buttonsContainer;       // ��������� ��� ������

    private BuildingSystem _buildingSystem; // ������ �� ������� �������������

    private void Start()
    {
        // �������� ������ �� ������� �������������
        _buildingSystem = FindObjectOfType<BuildingSystem>();

        // ������� ������ ��� ���� ��������� ������
        CreateBuildingButtons();
    }

    private void CreateBuildingButtons()
    {
        foreach (var data in _availableBuildings)
        {
            var btn = Instantiate(_buildingButtonPrefab, _buttonsContainer);
            btn.image.sprite = data.Icon;

            // �������� ��������� Tooltip
            var tooltip = btn.GetComponent<Tooltip>();
            if (tooltip == null)
            {
                Debug.LogError("Tooltip component missing on button prefab!");
                continue;
            }

            // ��������� �����
            var sb = new StringBuilder();
            sb.AppendLine(data.DisplayName);
            sb.AppendLine("Cost:");
            foreach (var res in data.ConstructionCost.Resources)
            {
                sb.AppendLine($"{res.Key}: {res.Value}");
            }

            tooltip.SetText(sb.ToString());

            btn.onClick.AddListener(() => _buildingSystem.StartBuildingPlacement(data));
        }
    }
}

