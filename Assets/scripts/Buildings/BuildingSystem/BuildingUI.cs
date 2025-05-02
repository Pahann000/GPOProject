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

    /// <summary>
    /// ������� ������ ��� ������� ���������� ������
    /// </summary>
    private void CreateBuildingButtons()
    {
        foreach (var building in _availableBuildings)
        {
            // ������� ����� ������
            var button = Instantiate(_buildingButtonPrefab, _buttonsContainer);

            // ������������� ������ ������
            button.GetComponent<Image>().sprite = building.Icon;

            // ��������� ���������� �����
            button.onClick.AddListener(() => _buildingSystem.StartBuildingPlacement(building));

            // ����� �������� Tooltip � ����������� � ������
            // button.GetComponent<TooltipTrigger>().SetTooltip(building.DisplayName, building.Description);
        }
    }
}

