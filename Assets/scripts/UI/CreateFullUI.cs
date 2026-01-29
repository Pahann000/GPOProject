using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CreateFullUI : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool createOnStart = true;
    [SerializeField] private BuildingData[] buildingDataArray;

    void Start()
    {
        if (createOnStart)
        {
            CreateCompleteUI();
        }
    }

    void CreateCompleteUI()
    {
        Debug.Log("=== СОЗДАНИЕ ПОЛНОГО UI ===");

        // 1. Создаем или находим Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.Log("Canvas не найден, создаю новый...");
            canvas = CreateCanvas();
        }

        // 2. Создаем ResourcePanel (панель ресурсов)
        CreateResourcePanel(canvas.transform);

        // 3. Создаем BuildingPanel (панель строительства)
        CreateBuildingPanel(canvas.transform);

        // 4. Создаем SelectionPanel (панель выделения)
        CreateSelectionPanel(canvas.transform);

        // 5. Создаем кнопку ToggleButton
        CreateToggleButton(canvas.transform);

        Debug.Log("Весь UI создан! Запустите игру и нажмите B для открытия панели строительства.");
    }

    Canvas CreateCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem если нет
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        return canvas;
    }

    void CreateResourcePanel(Transform canvasTransform)
    {
        Debug.Log("Создаю ResourcePanel...");

        GameObject panel = CreateUIPanel("ResourcePanel", canvasTransform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10),
            new Vector2(300, 100), new Color(0.1f, 0.1f, 0.2f, 0.9f));

        // Добавляем тексты ресурсов
        AddResourceText(panel.transform, "GoldText", "Золото: 1000", new Vector2(10, -10));
        AddResourceText(panel.transform, "WoodText", "Дерево: 500", new Vector2(10, -40));
        AddResourceText(panel.transform, "StoneText", "Камень: 200", new Vector2(10, -70));

        // Добавляем компонент ResourcePanelUI
        panel.AddComponent<ResourcePanelUI>();
    }

    void CreateBuildingPanel(Transform canvasTransform)
    {
        Debug.Log("Создаю BuildingPanel...");

        GameObject panel = CreateUIPanel("BuildingPanel", canvasTransform,
            new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-10, 0),
            new Vector2(300, 500), new Color(0.1f, 0.1f, 0.1f, 0.95f));

        // Делаем панель изначально скрытой
        panel.SetActive(false);

        // Добавляем заголовок
        GameObject title = CreateUIText("Title", panel.transform, "🏗️ Строительство",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10),
            new Vector2(250, 30), Color.yellow, 20, TextAnchor.UpperCenter);

        // Создаем контейнер для кнопок зданий
        GameObject content = CreateUIPanel("Content", panel.transform,
            new Vector2(0, 0), new Vector2(1, 1), Vector2.zero,
            new Vector2(-20, -50), Color.clear);

        // Добавляем ScrollView компоненты
        ScrollRect scrollRect = panel.AddComponent<ScrollRect>();
        scrollRect.content = content.GetComponent<RectTransform>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        // Добавляем маску
        Image maskImage = content.AddComponent<Image>();
        maskImage.color = new Color(0, 0, 0, 0.1f);
        Mask mask = content.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Добавляем компонент BuildingPanelUI
        BuildingPanelUI panelUI = panel.AddComponent<BuildingPanelUI>();

        // Создаем тестовые кнопки зданий если нет данных
        if (buildingDataArray == null || buildingDataArray.Length == 0)
        {
            CreateTestBuildingButtons(content.transform);
        }
    }

    void CreateSelectionPanel(Transform canvasTransform)
    {
        Debug.Log("Создаю SelectionPanel...");

        GameObject panel = CreateUIPanel("SelectionPanel", canvasTransform,
            new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10),
            new Vector2(400, 150), new Color(0.1f, 0.2f, 0.1f, 0.9f));

        // Делаем панель изначально скрытой
        panel.SetActive(false);

        // Добавляем информацию о выделенном объекте
        AddSelectionText(panel.transform, "NameText", "Выбрано: Ничего",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10),
            new Vector2(380, 30), Color.white, 18, TextAnchor.UpperCenter);

        AddSelectionText(panel.transform, "HealthText", "Здоровье: 100/100",
            new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10, 0),
            new Vector2(180, 30), Color.white, 14, TextAnchor.MiddleLeft);

        AddSelectionText(panel.transform, "InfoText", "Информация...",
            new Vector2(0.5f, 0), new Vector2(1, 0), new Vector2(-10, 10),
            new Vector2(380, 60), Color.white, 12, TextAnchor.UpperLeft);

        // Добавляем компонент SelectionPanelUI
        panel.AddComponent<SelectionPanelUI>();
    }

    void CreateToggleButton(Transform canvasTransform)
    {
        Debug.Log("Создаю ToggleButton...");

        GameObject button = CreateUIButton("ToggleButton", canvasTransform,
            new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -120),
            new Vector2(150, 40), new Color(0.2f, 0.4f, 0.8f, 1f), "🏗️ Строительство");

        // Настраиваем Button
        Button btn = button.GetComponent<Button>();
        btn.onClick.AddListener(() => {
            GameObject panel = GameObject.Find("BuildingPanel");
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
                Debug.Log($"Панель строительства: {panel.activeSelf}");
            }
        });
    }

    void CreateTestBuildingButtons(Transform container)
    {
        Debug.Log("Создаю тестовые кнопки зданий...");

        string[] buildingNames = { "Дом", "Казарма", "Ферма", "Кузница", "Склад" };
        Color[] colors = { Color.blue, Color.red, Color.green, Color.yellow, Color.cyan };

        for (int i = 0; i < buildingNames.Length; i++)
        {
            GameObject button = CreateUIButton($"BuildingButton_{i}", container,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10 - (i * 60)),
                new Vector2(250, 50), colors[i] * 0.3f, buildingNames[i]);

            Button btn = button.GetComponent<Button>();
            int index = i; // Для замыкания
            btn.onClick.AddListener(() => {
                Debug.Log($"Нажата кнопка строительства: {buildingNames[index]}");
            });
        }

        // Устанавливаем размер контейнера
        RectTransform containerRT = container.GetComponent<RectTransform>();
        containerRT.sizeDelta = new Vector2(250, buildingNames.Length * 60 + 20);
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

    GameObject CreateUIPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                           Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    GameObject CreateUIButton(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                            Vector2 position, Vector2 size, Color color, string text)
    {
        GameObject button = CreateUIPanel(name, parent, anchorMin, anchorMax, position, size, color);

        // Добавляем компонент Button
        Button btn = button.AddComponent<Button>();

        // Настраиваем цвета кнопки
        ColorBlock colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.selectedColor = color;
        btn.colors = colors;

        // Добавляем текст
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform);

        RectTransform textRT = textObj.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.color = Color.white;
        textComp.fontSize = 16;
        textComp.alignment = TextAlignmentOptions.Center;

        return button;
    }

    void AddResourceText(Transform parent, string name, string text, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(200, 30);

        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.color = Color.white;
        textComp.fontSize = 16;
        textComp.alignment = TextAlignmentOptions.Left;
    }

    GameObject CreateUIText(string name, Transform parent, string text,
                          Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
                          Vector2 size, Color color, int fontSize, TextAnchor alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent);

        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.color = color;
        textComp.fontSize = fontSize;

        // Преобразуем TextAnchor в TextAlignmentOptions
        switch (alignment)
        {
            case TextAnchor.UpperLeft: textComp.alignment = TextAlignmentOptions.TopLeft; break;
            case TextAnchor.UpperCenter: textComp.alignment = TextAlignmentOptions.Top; break;
            case TextAnchor.UpperRight: textComp.alignment = TextAlignmentOptions.TopRight; break;
            case TextAnchor.MiddleLeft: textComp.alignment = TextAlignmentOptions.Left; break;
            case TextAnchor.MiddleCenter: textComp.alignment = TextAlignmentOptions.Center; break;
            case TextAnchor.MiddleRight: textComp.alignment = TextAlignmentOptions.Right; break;
            case TextAnchor.LowerLeft: textComp.alignment = TextAlignmentOptions.BottomLeft; break;
            case TextAnchor.LowerCenter: textComp.alignment = TextAlignmentOptions.Bottom; break;
            case TextAnchor.LowerRight: textComp.alignment = TextAlignmentOptions.BottomRight; break;
        }

        return textObj;
    }

    void AddSelectionText(Transform parent, string name, string text,
                         Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
                         Vector2 size, Color color, int fontSize, TextAnchor alignment)
    {
        CreateUIText(name, parent, text, anchorMin, anchorMax, position, size, color, fontSize, alignment);
    }

    void Update()
    {
        // Нажмите B для открытия/закрытия панели строительства
        if (Input.GetKeyDown(KeyCode.B))
        {
            GameObject panel = GameObject.Find("BuildingPanel");
            if (panel != null)
            {
                panel.SetActive(!panel.activeSelf);
                Debug.Log($"Панель строительства: {panel.activeSelf}");
            }
        }

        // Нажмите R для обновления ресурсов
        if (Input.GetKeyDown(KeyCode.R))
        {
            UpdateResourceTexts();
        }
    }

    void UpdateResourceTexts()
    {
        // Обновляем тексты ресурсов (заглушка)
        GameObject goldText = GameObject.Find("GoldText");
        if (goldText != null)
        {
            TextMeshProUGUI text = goldText.GetComponent<TextMeshProUGUI>();
            text.text = $"Золото: {Random.Range(500, 2000)}";
        }
    }
}

