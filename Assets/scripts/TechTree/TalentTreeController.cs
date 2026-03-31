using System.Collections.Generic;
using UnityEngine;

public class TalentTreeController : MonoBehaviour
{
    [Header("Ссылки")]
    [SerializeField] private TalentManager techManager;
    [SerializeField] private ResourceManager resourceManager;

    [Header("Префабы UI")]
    [SerializeField] private GameObject technologyNodePrefab;
    [SerializeField] private Transform nodesContainer;

    [Header("Настройки отображения")]
    [SerializeField] private float horizontalSpacing = 200f;
    [SerializeField] private float verticalSpacing = 150f;

    private Dictionary<TalentData, TalentUI> nodeUIs;

    private void Start()
    {
        if (techManager == null)
            techManager = FindObjectOfType<TalentManager>();

        if (resourceManager == null)
            resourceManager = FindObjectOfType<ResourceManager>();

        nodeUIs = new Dictionary<TalentData, TalentUI>();

        CreateTechnologyTree();

        // Подписываемся на события
        if (techManager != null)
        {
            techManager.OnTechnologyTreeUpdated += RefreshTree;
        }
    }

    private void OnDestroy()
    {
        if (techManager != null)
        {
            techManager.OnTechnologyTreeUpdated -= RefreshTree;
        }
    }

    private void CreateTechnologyTree()
    {
        if (techManager == null) return;

        var allTechs = techManager.GetAllTechnologies();

        foreach (var tech in allTechs)
        {
            if (tech == null) continue;

            CreateTechnologyNode(tech);
        }

    }

    private void CreateTechnologyNode(TalentData technology)
    {
        GameObject nodeObj = Instantiate(technologyNodePrefab, nodesContainer);
        RectTransform rectTransform = nodeObj.GetComponent<RectTransform>();

        // Устанавливаем позицию из данных технологии
        rectTransform.anchoredPosition = technology.NodePosition * 100f; // Масштабируем

        TalentUI nodeUI = nodeObj.GetComponent<TalentUI>();
        nodeUI.Initialize(technology, techManager);

        nodeUIs[technology] = nodeUI;
    }

    private void RefreshTree()
    {
        foreach (var node in nodeUIs.Values)
        {
            node.UpdateUI();
        }
    }
}
