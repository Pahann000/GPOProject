using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class TalentTreeUI : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private TalentManager techManager;
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
        if (techManager == null)
            techManager = FindObjectOfType<TalentManager>();
        if (resourceManager == null)
            resourceManager = FindObjectOfType<ResourceManager>();

        // Получаем UIDocument
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("TechTreeUI: UIDocument component not found!");
            return;
        }

        root = uiDocument.rootVisualElement;

        // Применяем стили
        if (styleSheet != null)
            root.styleSheets.Add(styleSheet);

        // Ищем TreeContainer (он находится внутри MainPanel)
        var mainPanel = root.Q<VisualElement>("MainPanel");
        if (mainPanel != null)
        {
            treeContainer = mainPanel.Q<VisualElement>("TreeContainer");
        }
        else
        {
            // Если MainPanel не найден, ищем напрямую
            treeContainer = root.Q<VisualElement>("TreeContainer");
        }

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

    private void OnDestroy()
    {
        if (techManager != null)
        {
            techManager.OnTechnologyTreeChanged -= RefreshTree;
            techManager.OnTechnologyUnlocked -= RefreshTree;
        }
    }

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

        // Рисуем линии связей
        DrawConnections();
    }

    private void CreateTechNode(TalentData tech)
    {
        if (techNodeTemplate == null)
        {
            Debug.LogError("TechNodeTemplate не назначен!");
            return;
        }

        // Создаем элемент из шаблона
        VisualElement node = techNodeTemplate.Instantiate();
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
        if (costLabel != null && tech.cost != null && tech.cost.Length > 0)
        {
            string costText = "";
            foreach (var c in tech.cost)
            {
                costText += $"{c.type}: {c.amount}\n";
            }
            costLabel.text = costText;
        }

        // Устанавливаем иконку
        if (iconElement != null && tech.Icon != null)
        {
            iconElement.style.backgroundImage = new StyleBackground(tech.Icon);
        }

        // Настраиваем кнопку
        if (unlockButton != null)
        {
            unlockButton.clicked += () => techManager.UnlockTechnology(tech);
        }

        // Применяем класс состояния
        UpdateNodeState(node, tech);

        // Добавляем в контейнер
        treeContainer.Add(node);
        nodeElements[tech] = node;
    }

    private void UpdateNodeState(VisualElement node, TalentData tech)
    {
        // Удаляем старые классы
        node.RemoveFromClassList("locked");
        node.RemoveFromClassList("available");
        node.RemoveFromClassList("researched");

        Button button = node.Q<Button>("UnlockButton");

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

    private void DrawConnections()
    {
        if (treeContainer == null) return;

        foreach (var tech in techManager.allTechnologies)
        {
            if (tech == null || tech.Prerequisites == null) continue;

            foreach (var prereq in tech.Prerequisites)
            {
                if (prereq == null) continue;

                if (nodeElements.TryGetValue(prereq, out var fromNode) &&
                    nodeElements.TryGetValue(tech, out var toNode))
                {
                    DrawLine(fromNode, toNode);
                }
            }
        }
    }

    private void DrawLine(VisualElement fromNode, VisualElement toNode)
    {
        // Получаем позиции узлов
        float fromX = fromNode.resolvedStyle.left;
        float fromY = fromNode.resolvedStyle.top;
        float toX = toNode.resolvedStyle.left;
        float toY = toNode.resolvedStyle.top;

        // Центры узлов (предполагаем размер 140x160)
        float x1 = fromX + 70;
        float y1 = fromY + 80;
        float x2 = toX + 70;
        float y2 = toY + 80;

        // Вычисляем расстояние и угол
        float dx = x2 - x1;
        float dy = y2 - y1;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

        // Создаем линию
        VisualElement line = new VisualElement();
        line.style.position = Position.Absolute;
        line.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        line.style.height = 2;
        line.style.width = distance;
        line.style.left = x1;
        line.style.top = y1;
        line.style.rotate = new Rotate(Angle.Degrees(angle));

        treeContainer.Add(line);
    }
}
