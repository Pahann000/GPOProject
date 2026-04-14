using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Хранит методы создания UI.
/// </summary>
public class TalentTreeUI : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private TechTreeManager techManager;
    [SerializeField] private ResourceManager resourceManager;

    [Header("UI ресурсы")]
    [SerializeField] private VisualTreeAsset techNodeTemplate;
    [SerializeField] private StyleSheet styleSheet;

    private VisualElement root;
    private VisualElement treeContainer;
    private Dictionary<TalentData, VisualElement> nodeElements;

    private void Start()
    {
        // Ищем менеджеры
        if (techManager == null) techManager = FindObjectOfType<TechTreeManager>();
        if (resourceManager == null) resourceManager = FindObjectOfType<ResourceManager>();

        // Получаем UIDocument
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("TechTreeUI: UIDocument component not found!");
            return;
        }

        root = uiDocument.rootVisualElement;

        // Применяем стили
        if (styleSheet != null) root.styleSheets.Add(styleSheet);

        // Ищем TreeContainer
        treeContainer = root.Q<VisualElement>("TreeContainer");
        
        if (treeContainer == null)
        {
            Debug.LogError("TechTreeUI: TreeContainer not found! Check UXML structure.");
            return;
        }

        nodeElements = new Dictionary<TalentData, VisualElement>();

        // Подписываемся на события
        if (techManager != null)
        {
            techManager.OnTechnologyTreeChanged += RefreshTree;
            techManager.OnTechnologyUnlocked += RefreshTree;
        }

        RefreshTree();
    }

    /// <summary>
    /// Отписка от событий и т д
    /// </summary>
    private void OnDestroy()
    {
        if (techManager != null)
        {
            techManager.OnTechnologyTreeChanged -= RefreshTree;
            techManager.OnTechnologyUnlocked -= RefreshTree;
        }
    }

    /// <summary>
    /// Обновляет дерево при изменении.
    /// </summary>
    private void RefreshTree()
    {
        if (treeContainer == null) return;

        // Очищаем контейнер
        treeContainer.Clear();
        nodeElements.Clear();

        // Создаем узлы для всех технологий
        foreach (var tech in techManager.allTechnologies)
        {
            if (tech == null) continue;
            CreateTechNode(tech);
        }
    }

    /// <summary>
    /// Создает узел дерева.
    /// </summary>
    /// <param name="tech"> Объект узла.</param>
    private void CreateTechNode(TalentData tech)
    {

        if (techNodeTemplate == null)
        {
            Debug.LogError("TechNodeTemplate не назначен!");
            return;
        }

        // Создаем элемент из шаблона
        VisualElement node = techNodeTemplate.Instantiate();

        if (node == null)
        {
            Debug.LogError($"CreateTechNode: Failed to instantiate node for {tech.TalentName}");
            return;
        }

        node.userData = tech;
        node.name = $"Node_{tech.TalentName}";

        // Устанавливаем позицию
        node.style.position = Position.Absolute;
        node.style.left = tech.NodePosition.x;
        node.style.top = tech.NodePosition.y;

        // Заполняем данные
        Label nameLabel = node.Q<Label>("TechName");
        Label costLabel = node.Q<Label>("CostLabel");
        VisualElement iconElement = node.Q<VisualElement>("Icon");
        Button unlockButton = node.Q<Button>("UnlockButton");
        if (nameLabel != null) nameLabel.text = tech.TalentName;

        // Формируем текст стоимости
        if (costLabel != null) costLabel.text = tech.GetCostString();

        // Устанавливаем иконку
        if (iconElement != null && tech.Icon != null) iconElement.style.backgroundImage = new StyleBackground(tech.Icon);


        // Настраиваем кнопку
        if (unlockButton != null) unlockButton.clicked += () => techManager.UnlockTechnology(tech);

        // Применяем класс состояния
        UpdateNodeState(node, tech);

        // Добавляем в контейнер
        treeContainer.Add(node);
        nodeElements[tech] = node;

    }

    /// <summary>
    /// Обновляет состояние узла в процессе работы.
    /// </summary>
    /// <param name="node">Отображаемый узел. </param>
    /// <param name="tech">Объект в программе. </param>
    private void UpdateNodeState(VisualElement node, TalentData tech)
    {
        // Удаляем старые классы
        node.RemoveFromClassList("locked");
        node.RemoveFromClassList("available");
        node.RemoveFromClassList("researched");

        Button button = node.Q<Button>("UnlockButton");

        // Пометка узлов в списке.
        if (tech.IsUnlocked)
        {
            node.AddToClassList("researched");
            if (button != null)
            {
                button.SetEnabled(false);
                button.text = "✓ ИЗУЧЕНО";
            }
        }
        else if (techManager.CanUnlock(tech))
        {
            node.AddToClassList("available");
            if (button != null)
            {
                button.SetEnabled(true);
                button.text = "ИЗУЧИТЬ";
            }
        }
        else
        {
            node.AddToClassList("locked");
            if (button != null)
            {
                button.SetEnabled(false);
                button.text = "ЗАБЛОКИРОВАНО";
            }
        }
    }

}
