using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEditor.Events;
using UnityEngine.UI;

/// <summary>
/// Сборщик интерфейса: перестраивает MainUIPrefab в адаптивную структуру со
/// слоями (HUD, модальные окна, подсказки) и стилизует элементы.
/// </summary>
/// <remarks>
/// Меню <b>Tools/UI/Перестроить интерфейс</b>. Скрипт идемпотентен: каждый запуск
/// собирает структуру заново, поэтому UI правится кодом, а не вручную в YAML.
/// Адаптивность держится на якорях и layout-группах (никаких абсолютных позиций),
/// а окна открываются модально поверх затемнённого игрового поля.
/// </remarks>
public static class UIBuilder
{
    private const string PrefabPath = "Assets/Resources/MainUIPrefab.prefab";
    private const string SpriteRoot = "Assets/Sprites/UI/";

    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private static readonly Color TextPrimary = new Color(0.94f, 0.96f, 1f, 1f);
    private static readonly Color TextMuted = new Color(0.70f, 0.74f, 0.82f, 1f);
    private static readonly Color TextAccent = new Color(0.98f, 0.72f, 0.42f, 1f);

    private static TMP_FontAsset _fontAsset;
    private static Material _fontMaterial;

    [MenuItem("Tools/UI/Перестроить интерфейс", priority = 100)]
    public static void Rebuild()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);

        try
        {
            Build(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[UIBuilder] Интерфейс перестроен: адаптивный Canvas, слои HUD / модальные окна / подсказки.");
    }

    private static void Build(GameObject root)
    {
        SpriteHash sprites = new SpriteHash();

        // Если в префабе остался компонент с удалённым скриптом, SaveAsPrefabAsset
        // молча возвращает null и ничего не сохраняет — чистим такие записи заранее.
        int missing = 0;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            missing += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        }

        if (missing > 0) Debug.Log("[UIBuilder] Удалено компонентов с потерянными скриптами: " + missing);

        var existing = new Dictionary<string, Transform>();
        foreach (Transform child in root.transform) existing[child.name] = child;

        ConfigureCanvas(root);
        FindFont(root.transform);

        // ------------------------------------------------------------ Слои
        Transform hudLayer = EnsureLayer(root.transform, "HudLayer", 0);
        Transform modalLayer = EnsureLayer(root.transform, "ModalLayer", 1);
        Transform tooltipLayer = EnsureLayer(root.transform, "TooltipLayer", 2);

        // ------------------------------------------------------------ Верхняя панель HUD
        RectTransform topBar = EnsureChild(hudLayer, "TopBar", sprites.PanelWindow);
        AnchorTopStretch(topBar, 76f);

        var topLayout = EnsureComponent<HorizontalLayoutGroup>(topBar.gameObject);
        topLayout.padding = new RectOffset(20, 20, 8, 8);
        topLayout.spacing = 14f;
        topLayout.childAlignment = TextAnchor.MiddleLeft;
        topLayout.childControlWidth = true;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandWidth = false;
        topLayout.childForceExpandHeight = false;

        Transform factionLabel = MoveTo(existing, root.transform, "FactionLabel", topBar);
        if (factionLabel != null)
        {
            var rt = (RectTransform)factionLabel;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(430f, 48f);

            var layoutElement = EnsureComponent<LayoutElement>(factionLabel.gameObject);
            layoutElement.preferredWidth = 430f;
            layoutElement.preferredHeight = 48f;

            var label = factionLabel.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.fontSize = 30f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
            }
        }

        Transform resourcePanel = MoveTo(existing, root.transform, "ResourcePanel", topBar);
        if (resourcePanel != null)
        {
            var rt = (RectTransform)resourcePanel;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);

            var background = resourcePanel.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = null;
                background.color = Color.clear;
                background.raycastTarget = false;
            }

            var layout = resourcePanel.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.MiddleRight;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }
            else
            {
                var newLayout = resourcePanel.gameObject.AddComponent<HorizontalLayoutGroup>();
                newLayout.childAlignment = TextAnchor.MiddleRight;
                newLayout.spacing = 10f;
                newLayout.childControlWidth = false;
                newLayout.childControlHeight = false;
                newLayout.childForceExpandWidth = false;
                newLayout.childForceExpandHeight = false;
            }

            var layoutElement = EnsureComponent<LayoutElement>(resourcePanel.gameObject);
            layoutElement.flexibleWidth = 1f;
            layoutElement.preferredHeight = 52f;
        }

        // ------------------------------------------------------------ Нижняя панель действий
        RectTransform bottomBar = EnsureChild(hudLayer, "BottomBar", sprites.PanelWindow);
        AnchorBottomStretch(bottomBar, 104f);

        var bottomLayout = EnsureComponent<HorizontalLayoutGroup>(bottomBar.gameObject);
        bottomLayout.padding = new RectOffset(20, 20, 14, 14);
        bottomLayout.spacing = 16f;
        bottomLayout.childAlignment = TextAnchor.MiddleCenter;
        bottomLayout.childControlWidth = false;
        bottomLayout.childControlHeight = false;
        bottomLayout.childForceExpandWidth = false;
        bottomLayout.childForceExpandHeight = false;

        Button buildButton = CreateActionButton(bottomBar, "BuildButton", "🏗  Строительство", sprites.ButtonAccent, sprites.ButtonAccentHover, TextAccent);
        Button researchButton = CreateActionButton(bottomBar, "ResearchButton", "🔬  Исследования", sprites.ButtonNormal, sprites.ButtonHover, TextPrimary);

        // ------------------------------------------------------------ Модальный слой
        RectTransform modalRoot = EnsureChild(modalLayer, "ModalRoot", null);
        AnchorFull(modalRoot);

        var dimmerImage = EnsureComponent<Image>(modalRoot.gameObject);
        dimmerImage.sprite = sprites.Dimmer;
        dimmerImage.type = Image.Type.Sliced;
        dimmerImage.color = new Color(1f, 1f, 1f, 0.9f);
        dimmerImage.raycastTarget = true;

        var dimmerButton = EnsureComponent<Button>(modalRoot.gameObject);
        dimmerButton.transition = Selectable.Transition.None;
        dimmerButton.targetGraphic = dimmerImage;

        RectTransform panelHost = EnsureChild(modalRoot, "PanelHost", null);
        AnchorInset(panelHost, 0.06f, 0.08f, 0.94f, 0.92f);

        // ------------------------------------------------------------ Окна
        Transform buildingPanel = MoveTo(existing, root.transform, "BuildingPanel", panelHost);
        ModalPanelParts building = StyleModalPanel(buildingPanel, "Строительство", sprites);

        Transform researchPanel = MoveTo(existing, root.transform, "ResearchPanel", panelHost);
        ModalPanelParts research = StyleModalPanel(researchPanel, "Исследования", sprites);

        // Окно исследований: статус + полоса прогресса над списком.
        EnsureResearchStatus(research, sprites);

        // ------------------------------------------------------------ Панель выделения (док справа)
        Transform selectionPanel = MoveTo(existing, root.transform, "SelectionPanel", hudLayer);
        if (selectionPanel != null)
        {
            var rt = (RectTransform)selectionPanel;
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-20f, 120f);
            rt.sizeDelta = new Vector2(400f, 320f);

            var image = selectionPanel.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprites.PanelWindow;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
        }

        // ------------------------------------------------------------ Подсказки
        Transform tooltip = MoveTo(existing, root.transform, "TooltipPanel", tooltipLayer);
        if (tooltip != null)
        {
            var rt = (RectTransform)tooltip;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(360f, 150f);

            var image = tooltip.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = sprites.PanelWindow;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
        }

        // Старые кнопки-переключатели больше не нужны: окна открывает нижняя панель.
        RemoveLegacy(root.transform, existing, "ToggleButton");
        RemoveLegacy(root.transform, existing, "ResearchToggleButton");

        // Слои не должны перехватывать клики.
        foreach (Transform layer in new[] { hudLayer, modalLayer, tooltipLayer })
        {
            var image = layer.GetComponent<Image>();
            if (image != null) image.raycastTarget = false;
        }

        // ------------------------------------------------------------ Контроллер и менеджер
        var controller = EnsureComponent<UIPanelController>(root);
        var buildingUi = buildingPanel != null ? buildingPanel.GetComponent<BuildingPanelUI>() : null;
        var researchUi = researchPanel != null ? researchPanel.GetComponent<ResearchPanelUI>() : null;

        var controllerSo = new SerializedObject(controller);
        controllerSo.FindProperty("modalRoot").objectReferenceValue = modalRoot.gameObject;
        controllerSo.FindProperty("dimmerButton").objectReferenceValue = dimmerButton;

        SerializedProperty panelsProp = controllerSo.FindProperty("panels");
        int count = (buildingUi != null ? 1 : 0) + (researchUi != null ? 1 : 0);
        panelsProp.arraySize = count;

        int index = 0;
        if (buildingUi != null) panelsProp.GetArrayElementAtIndex(index++).objectReferenceValue = buildingUi;
        if (researchUi != null) panelsProp.GetArrayElementAtIndex(index).objectReferenceValue = researchUi;

        controllerSo.ApplyModifiedPropertiesWithoutUndo();

        var manager = root.GetComponent<UIManager>();
        if (manager != null)
        {
            var managerSo = new SerializedObject(manager);
            managerSo.FindProperty("panelController").objectReferenceValue = controller;
            managerSo.FindProperty("buildingPanel").objectReferenceValue = buildingUi;
            managerSo.FindProperty("researchPanel").objectReferenceValue = researchUi;
            managerSo.FindProperty("selectionPanel").objectReferenceValue = selectionPanel != null ? selectionPanel.GetComponent<SelectionPanelUI>() : null;
            managerSo.FindProperty("tooltip").objectReferenceValue = tooltip != null ? tooltip.GetComponent<AdvancedTooltip>() : null;
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            WireButton(buildButton, manager, "ToggleBuildingMenu");
            WireButton(researchButton, manager, "ToggleResearchMenu");
        }

        // ------------------------------------------------------------ Привязка полей панелей
        WirePanelBase(buildingUi, building);
        WirePanelBase(researchUi, research);

        StyleRowPrefabs(sprites);
    }

    // ---------------------------------------------------------------- Canvas

    private static void ConfigureCanvas(GameObject root)
    {
        var scaler = EnsureComponent<CanvasScaler>(root);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        EnsureComponent<GraphicRaycaster>(root);
    }

    private static void FindFont(Transform root)
    {
        if (_fontAsset != null) return;

        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font == null) continue;

            _fontAsset = text.font;
            _fontMaterial = text.fontSharedMaterial;
            return;
        }
    }

    // ---------------------------------------------------------------- структура

    private static Transform EnsureLayer(Transform parent, string name, int siblingIndex)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            existing = go.transform;
            existing.SetParent(parent, false);
        }

        AnchorFull((RectTransform)existing);
        existing.SetSiblingIndex(siblingIndex);
        return existing;
    }

    private static RectTransform EnsureChild(Transform parent, string name, Sprite background)
    {
        Transform existing = parent.Find(name);
        RectTransform rt;

        if (existing == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
        }
        else
        {
            rt = (RectTransform)existing;
        }

        if (background != null)
        {
            var image = EnsureComponent<Image>(rt.gameObject);
            image.sprite = background;
            image.type = Image.Type.Sliced;
            image.color = new Color(1f, 1f, 1f, 0.94f);
        }

        return rt;
    }

    private static Transform MoveTo(Dictionary<string, Transform> index, Transform root, string name, Transform parent)
    {
        Transform source = null;

        if (index.TryGetValue(name, out Transform known) && known != null) source = known;
        if (source == null) source = root.Find(name);
        if (source == null) source = FindDeep(root, name);

        if (source == null)
        {
            Debug.LogWarning("[UIBuilder] Не найден объект " + name);
            return null;
        }

        // Уже в нужном месте — не дёргаем иерархию лишний раз.
        if (source.parent != parent) source.SetParent(parent, false);

        index[name] = source;
        return source;
    }

    private static void RemoveLegacy(Transform root, Dictionary<string, Transform> index, string name)
    {
        if (index.TryGetValue(name, out Transform known) && known != null)
        {
            Object.DestroyImmediate(known.gameObject);
            index.Remove(name);
            return;
        }

        Transform found = root.Find(name);
        if (found != null) Object.DestroyImmediate(found.gameObject);
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static void AnchorFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.localScale = Vector3.one;
    }

    private static void AnchorTopStretch(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -height);
        rt.offsetMax = Vector2.zero;
    }

    private static void AnchorBottomStretch(RectTransform rt, float height)
    {
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0f, height);
    }

    private static void AnchorInset(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
    }

    // ---------------------------------------------------------------- кнопки

    private static Button CreateActionButton(Transform parent, string name, string label, Sprite normal, Sprite hover, Color labelColor)
    {
        Transform existing = parent.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = new Vector2(320f, 70f);

        var image = go.AddComponent<Image>();
        image.sprite = normal;
        image.type = Image.Type.Sliced;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState state = button.spriteState;
        state.highlightedSprite = hover;
        state.pressedSprite = hover;
        button.spriteState = state;

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 320f;
        layoutElement.preferredHeight = 70f;

        TextMeshProUGUI text = CreateText(rt, "Label", label, 28f, labelColor, TextAlignmentOptions.Center);
        AnchorFull(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(10f, 0f);
        text.rectTransform.offsetMax = new Vector2(-10f, 0f);
        text.raycastTarget = false;

        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.richText = true;

        if (_fontAsset != null) text.font = _fontAsset;
        if (_fontMaterial != null) text.fontSharedMaterial = _fontMaterial;

        return text;
    }

    private static void WireButton(Button button, UIManager manager, string methodName)
    {
        if (button == null || manager == null) return;

        button.onClick.RemoveAllListeners();
        System.Reflection.MethodInfo method = typeof(UIManager).GetMethod(methodName);
        if (method == null) return;

        UnityEventTools.AddVoidPersistentListener(button.onClick, (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), manager, method));
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;

        // Объекты могли быть перенесены прошлым запуском сборщика, поэтому ищем
        // на любой глубине, а не только среди прямых детей.
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name) return child;
        }

        return null;
    }

    /// <summary>
    /// Создаёт вертикальный список с прокруткой (ScrollRect + Mask + Scrollbar).
    /// Нужен, если в старых версиях префаба список был утерян.
    /// </summary>
    private static Transform CreateScrollList(Transform parent, string name, SpriteHash sprites)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = Color.clear;

        var scrollRect = go.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 28f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform));
        var viewportRt = (RectTransform)viewportGo.transform;
        viewportRt.SetParent(rt, false);
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.pivot = new Vector2(0f, 1f);
        viewportRt.offsetMin = new Vector2(0f, 0f);
        viewportRt.offsetMax = new Vector2(-16f, 0f);

        var viewportImage = viewportGo.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        viewportGo.AddComponent<RectMask2D>();

        var contentGo = new GameObject("Content", typeof(RectTransform));
        var contentRt = (RectTransform)contentGo.transform;
        contentRt.SetParent(viewportRt, false);
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;

        scrollRect.viewport = viewportRt;
        scrollRect.content = contentRt;

        // Полоса прокрутки — узкая, справа.
        var scrollbarGo = new GameObject("Scrollbar Vertical", typeof(RectTransform));
        var scrollbarRt = (RectTransform)scrollbarGo.transform;
        scrollbarRt.SetParent(rt, false);
        scrollbarRt.anchorMin = new Vector2(1f, 0f);
        scrollbarRt.anchorMax = new Vector2(1f, 1f);
        scrollbarRt.pivot = new Vector2(1f, 0.5f);
        scrollbarRt.sizeDelta = new Vector2(14f, 0f);
        scrollbarRt.offsetMin = new Vector2(-14f, 0f);
        scrollbarRt.offsetMax = Vector2.zero;

        var scrollbarImage = scrollbarGo.AddComponent<Image>();
        scrollbarImage.sprite = sprites.ProgressBg;
        scrollbarImage.type = Image.Type.Sliced;
        scrollbarImage.color = new Color(1f, 1f, 1f, 0.35f);

        var scrollbar = scrollbarGo.AddComponent<Scrollbar>();
        scrollbar.targetGraphic = scrollbarImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var slidingGo = new GameObject("Sliding Area", typeof(RectTransform));
        var slidingRt = (RectTransform)slidingGo.transform;
        slidingRt.SetParent(scrollbarRt, false);
        AnchorFull(slidingRt);
        slidingRt.offsetMin = new Vector2(3f, 3f);
        slidingRt.offsetMax = new Vector2(-3f, -3f);

        var handleGo = new GameObject("Handle", typeof(RectTransform));
        var handleRt = (RectTransform)handleGo.transform;
        handleRt.SetParent(slidingRt, false);
        AnchorFull(handleRt);

        var handleImage = handleGo.AddComponent<Image>();
        handleImage.sprite = sprites.ButtonNormal;
        handleImage.type = Image.Type.Sliced;

        scrollbar.handleRect = handleRt;

        StyleListContent(contentRt);
        return rt;
    }

    // ---------------------------------------------------------------- окна

    private class ModalPanelParts
    {
        public RectTransform Root;
        public VerticalLayoutGroup Layout;
        public TextMeshProUGUI Title;
        public Button CloseButton;
        public RectTransform Body;
        public RectTransform ListContent;
        public ScrollRect Scroll;
    }

    private static ModalPanelParts StyleModalPanel(Transform panel, string title, SpriteHash sprites)
    {
        var parts = new ModalPanelParts();
        if (panel == null) return parts;

        parts.Root = (RectTransform)panel;
        AnchorFull(parts.Root);

        var background = EnsureComponent<Image>(panel.gameObject);
        background.sprite = sprites.PanelWindow;
        background.type = Image.Type.Sliced;
        background.color = Color.white;

        parts.Layout = EnsureComponent<VerticalLayoutGroup>(panel.gameObject);
        parts.Layout.padding = new RectOffset(0, 0, 0, 0);
        parts.Layout.spacing = 0f;
        parts.Layout.childControlWidth = true;
        parts.Layout.childControlHeight = true;
        parts.Layout.childForceExpandWidth = true;
        parts.Layout.childForceExpandHeight = false;

        // --- Шапка
        Transform oldHeader = panel.Find("PanelHeader");
        if (oldHeader != null) Object.DestroyImmediate(oldHeader.gameObject);

        var headerGo = new GameObject("PanelHeader", typeof(RectTransform));
        var headerRt = (RectTransform)headerGo.transform;
        headerRt.SetParent(parts.Root, false);

        var headerImage = headerGo.AddComponent<Image>();
        headerImage.sprite = sprites.PanelHeader;
        headerImage.type = Image.Type.Sliced;

        var headerLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(20, 14, 10, 10);
        headerLayout.spacing = 12f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = false;

        var headerLayoutElement = headerGo.AddComponent<LayoutElement>();
        headerLayoutElement.preferredHeight = 76f;
        headerLayoutElement.flexibleWidth = 1f;

        parts.Title = CreateText(headerRt, "Title", title, 32f, TextPrimary, TextAlignmentOptions.MidlineLeft);
        var titleLayoutElement = parts.Title.gameObject.AddComponent<LayoutElement>();
        titleLayoutElement.flexibleWidth = 1f;

        // --- Кнопка закрытия
        var closeGo = new GameObject("CloseButton", typeof(RectTransform));
        var closeRt = (RectTransform)closeGo.transform;
        closeRt.SetParent(headerRt, false);
        closeRt.sizeDelta = new Vector2(52f, 48f);

        var closeImage = closeGo.AddComponent<Image>();
        closeImage.sprite = sprites.ButtonNormal;
        closeImage.type = Image.Type.Sliced;

        parts.CloseButton = closeGo.AddComponent<Button>();
        parts.CloseButton.targetGraphic = closeImage;
        parts.CloseButton.transition = Selectable.Transition.SpriteSwap;
        SpriteState closeState = parts.CloseButton.spriteState;
        closeState.highlightedSprite = sprites.ButtonHover;
        closeState.pressedSprite = sprites.ButtonPressed;
        parts.CloseButton.spriteState = closeState;

        var closeLayoutElement = closeGo.AddComponent<LayoutElement>();
        closeLayoutElement.preferredWidth = 52f;
        closeLayoutElement.preferredHeight = 48f;

        TextMeshProUGUI closeLabel = CreateText(closeRt, "Label", "✕", 26f, TextMuted, TextAlignmentOptions.Center);
        AnchorFull(closeLabel.rectTransform);
        closeLabel.raycastTarget = false;

        // --- Тело окна (растягивается на остаток высоты)
        Transform oldBody = panel.Find("Body");
        if (oldBody != null) Object.DestroyImmediate(oldBody.gameObject);

        var bodyGo = new GameObject("Body", typeof(RectTransform));
        parts.Body = (RectTransform)bodyGo.transform;
        parts.Body.SetParent(parts.Root, false);

        var bodyLayout = bodyGo.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(16, 16, 16, 16);
        bodyLayout.spacing = 12f;
        bodyLayout.childAlignment = TextAnchor.UpperCenter;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        var bodyLayoutElement = bodyGo.AddComponent<LayoutElement>();
        bodyLayoutElement.flexibleHeight = 1f;

        // --- Список: переносим существующий скролл внутрь тела (или создаём заново)
        Transform scroll = FindDeep(panel, "BuildingButtonScroll");
        if (scroll == null)
        {
            scroll = CreateScrollList(parts.Body, "BuildingButtonScroll", sprites);
        }
        if (scroll != null)
        {
            var scrollRt = (RectTransform)scroll;
            scrollRt.SetParent(parts.Body, false);

            // Раскладку скролла задаёт LayoutGroup тела — сбрасываем остатки
            // абсолютной разметки (их могло накопиться от старых версий префаба).
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.pivot = new Vector2(0.5f, 0.5f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollRt.anchoredPosition = Vector2.zero;

            var scrollImage = scroll.GetComponent<Image>();
            if (scrollImage != null)
            {
                scrollImage.color = Color.clear;
                scrollImage.raycastTarget = true;
            }

            var scrollLayoutElement = EnsureComponent<LayoutElement>(scroll.gameObject);
            scrollLayoutElement.flexibleHeight = 1f;
            scrollLayoutElement.minHeight = 160f;

            var scrollRect = EnsureComponent<ScrollRect>(scroll.gameObject);
            Transform viewport = scroll.Find("Viewport");
            if (viewport != null)
            {
                scrollRect.viewport = (RectTransform)viewport;

                Transform inner = viewport.Find("Content");
                if (inner != null)
                {
                    scrollRect.content = (RectTransform)inner;
                    StyleListContent((RectTransform)inner);
                    parts.ListContent = (RectTransform)inner;
                }
            }

            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 28f;
            parts.Scroll = scrollRect;
        }

        // Чистим прежние служебные строки: статус исследования пересоздаётся
        // внутри тела окна (EnsureResearchStatus), а старая версия могла остаться
        // как прямой дочерний объект панели.
        Transform oldStatus = FindDeep(parts.Body, "ResearchStatusRow");
        if (oldStatus != null) Object.DestroyImmediate(oldStatus.gameObject);

        foreach (Transform candidate in parts.Root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate != parts.Root && candidate.name == "ResearchStatus" && candidate.parent == parts.Root)
            {
                Object.DestroyImmediate(candidate.gameObject);
            }
        }

        return parts;
    }

    private static void EnsureResearchStatus(ModalPanelParts parts, SpriteHash sprites)
    {
        if (parts == null || parts.Body == null || parts.ListContent == null) return;

        var row = new GameObject("ResearchStatusRow", typeof(RectTransform));
        var rowRt = (RectTransform)row.transform;
        rowRt.SetParent(parts.Body, false);
        rowRt.SetSiblingIndex(0);

        var rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 14f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        var rowLayoutElement = row.AddComponent<LayoutElement>();
        rowLayoutElement.preferredHeight = 52f;
        rowLayoutElement.flexibleWidth = 1f;

        TextMeshProUGUI status = CreateText(rowRt, "ResearchStatus", "Выберите исследование", 24f, TextPrimary, TextAlignmentOptions.MidlineLeft);
        var statusLayoutElement = status.gameObject.AddComponent<LayoutElement>();
        statusLayoutElement.flexibleWidth = 1f;

        // Полоса прогресса: фон + заливка (ширина заливки управляется из кода).
        var barGo = new GameObject("ProgressBg", typeof(RectTransform));
        var barRt = (RectTransform)barGo.transform;
        barRt.SetParent(rowRt, false);
        barRt.sizeDelta = new Vector2(320f, 26f);

        var barImage = barGo.AddComponent<Image>();
        barImage.sprite = sprites.ProgressBg;
        barImage.type = Image.Type.Sliced;

        var barLayoutElement = barGo.AddComponent<LayoutElement>();
        barLayoutElement.preferredWidth = 320f;
        barLayoutElement.preferredHeight = 26f;

        var fillGo = new GameObject("ProgressFill", typeof(RectTransform));
        var fillRt = (RectTransform)fillGo.transform;
        fillRt.SetParent(barRt, false);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(0f, 1f);
        fillRt.offsetMin = new Vector2(3f, 3f);
        fillRt.offsetMax = new Vector2(-3f, -3f);

        var fillImage = fillGo.AddComponent<Image>();
        fillImage.sprite = sprites.ProgressFill;
        fillImage.type = Image.Type.Sliced;
        fillImage.raycastTarget = false;

        // Привязываем поля панели исследований: статус и заливку прогресса.
        var researchPanel = parts.Root.GetComponent<ResearchPanelUI>();
        if (researchPanel != null)
        {
            var so = new SerializedObject(researchPanel);
            so.FindProperty("researchEntryPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/ResearchEntry.prefab");
            so.FindProperty("entriesContainer").objectReferenceValue = parts.ListContent;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("progressFill").objectReferenceValue = fillImage;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void StyleListContent(RectTransform content)
    {
        if (content == null) return;

        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var layout = EnsureComponent<VerticalLayoutGroup>(content.gameObject);
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = EnsureComponent<ContentSizeFitter>(content.gameObject);
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private static void WirePanelBase(UIPanelBase panel, ModalPanelParts parts)
    {
        if (panel == null || parts == null) return;

        var so = new SerializedObject(panel);
        so.FindProperty("titleText").objectReferenceValue = parts.Title;
        so.FindProperty("closeButton").objectReferenceValue = parts.CloseButton;

        if (panel is BuildingPanelUI buildingPanel)
        {
            so.FindProperty("buildingButtonPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/anotherTest.prefab");
            so.FindProperty("buttonsContainer").objectReferenceValue = parts.ListContent;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------------------------------------------------------- строки списков

    private static void StyleRowPrefabs(SpriteHash sprites)
    {
        StyleRowPrefab("Assets/Prefabs/UI/anotherTest.prefab", sprites, isBuilding: true);
        StyleRowPrefab("Assets/Prefabs/UI/ResearchEntry.prefab", sprites, isBuilding: false);
    }

    private static void StyleRowPrefab(string path, SpriteHash sprites, bool isBuilding)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var rt = (RectTransform)root.transform;
            rt.sizeDelta = new Vector2(0f, isBuilding ? 84f : 76f);

            var layoutElement = EnsureComponent<LayoutElement>(root);
            layoutElement.preferredHeight = isBuilding ? 84f : 76f;
            layoutElement.flexibleWidth = 1f;

            Transform background = root.transform.Find("BackgroundButton");
            if (background != null)
            {
                var image = background.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprites.RowNormal;
                    image.type = Image.Type.Sliced;
                    image.color = Color.white;
                }

                AnchorFull((RectTransform)background);
            }

            var rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.sprite = sprites.RowNormal;
                rootImage.type = Image.Type.Sliced;
                rootImage.color = Color.white;
            }

            if (isBuilding)
            {
                var button = root.GetComponent<BuildingButtonUI>();
                if (button != null)
                {
                    var so = new SerializedObject(button);
                    so.FindProperty("stateNormal").objectReferenceValue = sprites.RowNormal;
                    so.FindProperty("stateHover").objectReferenceValue = sprites.RowHover;
                    so.FindProperty("stateLocked").objectReferenceValue = sprites.RowLocked;
                    so.FindProperty("stateUnaffordable").objectReferenceValue = sprites.RowInProgress;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            else
            {
                var button = root.GetComponent<ResearchButtonUI>();
                if (button != null)
                {
                    var so = new SerializedObject(button);
                    so.FindProperty("stateAvailable").objectReferenceValue = sprites.RowNormal;
                    so.FindProperty("stateHover").objectReferenceValue = sprites.RowHover;
                    so.FindProperty("stateInProgress").objectReferenceValue = sprites.RowInProgress;
                    so.FindProperty("stateCompleted").objectReferenceValue = sprites.RowCompleted;
                    so.FindProperty("stateLocked").objectReferenceValue = sprites.RowLocked;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // ---------------------------------------------------------------- спрайты

    private class SpriteHash
    {
        public readonly Sprite PanelWindow = Load("panel_window");
        public readonly Sprite PanelHeader = Load("panel_header");
        public readonly Sprite ButtonNormal = Load("button_normal");
        public readonly Sprite ButtonHover = Load("button_hover");
        public readonly Sprite ButtonPressed = Load("button_pressed");
        public readonly Sprite ButtonAccent = Load("button_accent");
        public readonly Sprite ButtonAccentHover = Load("button_accent_hover");
        public readonly Sprite RowNormal = Load("row_normal");
        public readonly Sprite RowHover = Load("row_hover");
        public readonly Sprite RowCompleted = Load("row_completed");
        public readonly Sprite RowInProgress = Load("row_in_progress");
        public readonly Sprite RowLocked = Load("row_locked");
        public readonly Sprite ProgressBg = Load("progress_bg");
        public readonly Sprite ProgressFill = Load("progress_fill");
        public readonly Sprite Dimmer = Load("dimmer");

        private static Sprite Load(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + name + ".png");
            if (sprite == null) Debug.LogWarning("[UIBuilder] Не найден спрайт " + name);
            return sprite;
        }
    }
}
