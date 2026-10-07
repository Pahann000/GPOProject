using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Хранит методы создания UI.
/// </summary>
public class TalentTreeUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset techNodeTemplate;
    [SerializeField] private StyleSheet styleSheet;
    [SerializeField] private Player localPlayer;

    private GameKernel _kernel;
    private TechTreeSystem _techTreeSystem;
    private VisualElement _treeContainer;
    private Dictionary<TalentData, VisualElement> _nodeElements = new();
    private bool _isInitialized = false;

    //private void Start()
    //{
    //    _kernel = GameKernel.Instance;
    //    _techTreeSystem = _kernel.GetSystem<TechTreeSystem>();

    //    var root = uiDocument.rootVisualElement;
    //    if (styleSheet != null) root.styleSheets.Add(styleSheet);

    //    _treeContainer = root.Q<VisualElement>("TreeContainer");
    //    if (_treeContainer == null)
    //    {
    //        Debug.LogError("TreeContainer не найден!");
    //        return;
    //    }

    //    // Подписки
    //    _kernel.EventBus.Subscribe<TechnologyTreeChangedEvent>(OnTreeChanged);
    //    _kernel.EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);

    //    BuildTree();
    //}

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!_isInitialized)
            TryInitialize();
    }

    private void TryInitialize()
    {
        // 1. Проверяем ядро
        if (GameKernel.Instance == null)
        {
            Debug.Log("[TalentTreeUI] Ожидаю GameKernel...");
            return;
        }
        _kernel = GameKernel.Instance;

        // 2. Проверяем систему дерева
        _techTreeSystem = _kernel.GetSystem<TechTreeSystem>();
        if (_techTreeSystem == null)
        {
            Debug.Log("[TalentTreeUI] Ожидаю TechTreeSystem...");
            return;
        }

        // 3. Проверяем UI
        if (uiDocument == null)
        {
            Debug.LogError("[TalentTreeUI] UIDocument не назначен!");
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.Log("[TalentTreeUI] Ожидаю rootVisualElement...");
            return;
        }

        if (styleSheet != null) root.styleSheets.Add(styleSheet);

        _treeContainer = root.Q<VisualElement>("TreeContainer");
        if (_treeContainer == null)
        {
            Debug.LogError("[TalentTreeUI] TreeContainer не найден!");
            return;
        }

        // 4. Пытаемся найти игрока, если не назначен
        if (localPlayer == null)
        {
            // Пробуем через заглушку
            if (PlayerStub.Instance != null)
                localPlayer = PlayerStub.Instance.LocalPlayer;

            // Или через FindObjectOfType
            if (localPlayer == null)
                localPlayer = FindObjectOfType<Player>();

            if (localPlayer != null)
                Debug.Log($"[TalentTreeUI] Найден игрок: {localPlayer.netId}");
            else
                Debug.LogWarning("[TalentTreeUI] Игрок не найден, работаем в режиме заглушки");
        }

        // 5. Подписки на события
        _kernel.EventBus.Subscribe<TechnologyTreeChangedEvent>(OnTreeChanged);
        _kernel.EventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);

        _isInitialized = true;
        Debug.Log("[TalentTreeUI] Инициализирован");

        BuildTree();
    }

    private void OnDestroy()
    {
        if (_kernel == null) return;
        _kernel.EventBus.Unsubscribe<TechnologyTreeChangedEvent>(OnTreeChanged);
        _kernel.EventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
    }

    private void OnTreeChanged(TechnologyTreeChangedEvent evt)
    {
        //if (evt.Player != localPlayer) return;
        BuildTree();
    }

    private void OnResourceChanged(ResourceChangedEvent evt)
    {
        //if (evt.Player != localPlayer) return;
        UpdateAllNodes();
    }

    private void BuildTree()
    {
        _treeContainer.Clear();
        _nodeElements.Clear();

        foreach (var tech in _techTreeSystem.GetAllTechnologies())
        {
            CreateTechNode(tech);
        }

        DrawConnections();
    }

    private void CreateTechNode(TalentData tech)
    {
        var node = techNodeTemplate.Instantiate();
        node.userData = tech;
        node.style.position = Position.Absolute;
        node.style.left = tech.Position.x;
        node.style.top = tech.Position.y;

        var nameLabel = node.Q<Label>("TechName");
        var costLabel = node.Q<Label>("CostLabel");
        var unlockButton = node.Q<Button>("UnlockButton");
        var icon = node.Q<VisualElement>("Icon");

        if (nameLabel != null) nameLabel.text = tech.TalentName;
        if (costLabel != null) costLabel.text = GetCostText(tech);
        if (icon != null && tech.Icon != null)
            icon.style.backgroundImage = new StyleBackground(tech.Icon);

        if (unlockButton != null)
            unlockButton.clicked += () => OnUnlockClicked(tech);

        UpdateNodeState(node, tech);
        _treeContainer.Add(node);
        _nodeElements[tech] = node;
    }

    private void OnUnlockClicked(TalentData tech)
    {
        // Если игрока нет — используем заглушку
        Player player = localPlayer;

        if (player == null && PlayerStub.Instance != null)
            player = PlayerStub.Instance.LocalPlayer;

        if (player == null)
        {
            Debug.LogWarning($"[TalentTreeUI] Нет игрока для исследования {tech.TalentName}. Исследуем напрямую (временно).");
            // Временное решение: разблокируем напрямую, без списания ресурсов
            tech.IsUnlocked = true;
            _kernel.EventBus.Raise(new TechnologyTreeChangedEvent(null));
            return;
        }

        _techTreeSystem.UnlockTechnology(player, tech);
    }

    private void UpdateNodeState(VisualElement node, TalentData tech)
    {
        var button = node.Q<Button>("UnlockButton");
        node.RemoveFromClassList("locked");
        node.RemoveFromClassList("available");
        node.RemoveFromClassList("researched");

        if (tech.IsUnlocked)
        {
            node.AddToClassList("researched");
            if (button != null) { button.SetEnabled(false); button.text = "✓ ИЗУЧЕНО"; }
        }
        else if (CanUnlockForCurrentPlayer(tech))
        {
            node.AddToClassList("available");
            if (button != null) { button.SetEnabled(true); button.text = "ИЗУЧИТЬ"; }
        }
        else
        {
            node.AddToClassList("locked");
            if (button != null) { button.SetEnabled(false); button.text = "🔒"; }
        }
    }

    private bool CanUnlockForCurrentPlayer(TalentData tech)
    {
        Player player = localPlayer;

        if (player == null && PlayerStub.Instance != null)
            player = PlayerStub.Instance.LocalPlayer;

        // Если игрока нет — временно разрешаем исследование (только проверка зависимостей)
        if (player == null)
        {
            if (tech.Prerequisites != null)
            {
                foreach (var prereq in tech.Prerequisites)
                {
                    if (prereq == null || !prereq.IsUnlocked) return false;
                }
            }
            return true; // временно: ресурсы не проверяем
        }

        return _techTreeSystem.CanUnlock(player, tech);
    }

    private void UpdateAllNodes()
    {
        foreach (var kvp in _nodeElements)
            UpdateNodeState(kvp.Value, kvp.Key);
    }

    private string GetCostText(TalentData tech)
    {
        if (tech.Cost?.Resources == null || tech.Cost.Resources.Count == 0)
            return "";

        string result = "";
        foreach (var kvp in tech.Cost.Resources)
            result += $"{kvp.Key}: {kvp.Value}\n";
        return result;
    }

    private void DrawConnections()
    {
        foreach (var tech in _techTreeSystem.GetAllTechnologies())
        {
            if (tech.Prerequisites == null) continue;
            foreach (var prereq in tech.Prerequisites)
            {
                if (prereq == null) continue;
                if (_nodeElements.TryGetValue(prereq, out var fromNode) &&
                    _nodeElements.TryGetValue(tech, out var toNode))
                {
                    DrawLine(fromNode, toNode);
                }
            }
        }
    }

    private void DrawLine(VisualElement from, VisualElement to)
    {
        float x1 = from.resolvedStyle.left + 80;
        float y1 = from.resolvedStyle.top + 90;
        float x2 = to.resolvedStyle.left + 80;
        float y2 = to.resolvedStyle.top + 90;

        float dx = x2 - x1;
        float dy = y2 - y1;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

        var line = new VisualElement();
        line.style.position = Position.Absolute;
        line.style.backgroundColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
        line.style.height = 2;
        line.style.width = distance;
        line.style.left = x1;
        line.style.top = y1;
        line.style.rotate = new Rotate(Angle.Degrees(angle));

        _treeContainer.Insert(0, line);
    }
}
