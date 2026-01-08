using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class BuildingUI : MonoBehaviour
{
    [Header("Building Settings")]
    [SerializeField] private BuildingData[] _availableBuildings;
    [SerializeField] private Button _buildingButtonPrefab;
    [SerializeField] private Transform _buttonsContainer;

    private BuildingSystem _buildingSystem;

    private void Start()
    {
        _buildingSystem = FindObjectOfType<BuildingSystem>();
        CreateBuildingButtons();
    }

    private void CreateBuildingButtons()
    {
        if (_availableBuildings == null || _availableBuildings.Length == 0)
        {
            Debug.LogError("Available Buildings array is empty!");
            return;
        }

        if (_buildingButtonPrefab == null)
        {
            Debug.LogError("Building Button Prefab is not assigned!");
            return;
        }

        foreach (var data in _availableBuildings)
        {
            if (data == null)
            {
                Debug.LogError("Found null in _availableBuildings array!");
                continue;
            }

            if (data.ConstructionCost.Resources == null || data.ConstructionCost.Resources.Count == 0)
            {
                Debug.LogError($"Building {data.DisplayName} has no construction cost!");
                continue;
            }

            var btn = Instantiate(_buildingButtonPrefab, _buttonsContainer);
            btn.image.sprite = data.Icon;

            var tooltip = btn.GetComponent<Tooltip>();
            if (tooltip == null)
            {
                Debug.LogError("Tooltip component missing on button prefab!");
                continue;
            }

            var sb = new StringBuilder();
            sb.AppendLine(data.DisplayName);
            sb.AppendLine("Cost:");

            foreach (var res in data.ConstructionCost.Resources)
            {
                sb.AppendLine($"{res.Type}: {res.Amount}");
            }

            tooltip.SetText(sb.ToString());
            btn.onClick.AddListener(() => _buildingSystem.StartBuildingPlacement(data));
        }
    }
}