using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIVisibilityDebugger : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool debugOnStart = true;
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;

    private Canvas canvas;

    void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();

        if (debugOnStart)
        {
            Debug.Log("=== ДИАГНОСТИКА ВИДИМОСТИ UI ===");
            CheckAllUIElements();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            CheckAllUIElements();
        }
    }

    void CheckAllUIElements()
    {
        if (canvas == null)
        {
            Debug.LogError("Canvas не найден!");
            return;
        }

        Debug.Log($"Canvas: {canvas.name}, Render Mode: {canvas.renderMode}, Active: {canvas.gameObject.activeSelf}");
        Debug.Log($"Canvas Scaler: {canvas.GetComponent<CanvasScaler>() != null}");
        Debug.Log($"Screen: {Screen.width}x{Screen.height}");

        // Проверяем все UI элементы на Canvas
        RectTransform[] allUI = canvas.GetComponentsInChildren<RectTransform>(true);
        Debug.Log($"Всего UI элементов: {allUI.Length}");

        foreach (RectTransform rt in allUI)
        {
            if (rt == canvas.transform) continue; // Пропускаем сам Canvas

            string status = GetUIElementStatus(rt);
            Debug.Log(status);

            // Если элемент должен быть видим, но его нет на экране
            if (rt.gameObject.activeInHierarchy && IsRectTransformOffScreen(rt))
            {
                Debug.LogWarning($"⚠️ {rt.name} ЗА ПРЕДЕЛАМИ ЭКРАНА!");
            }
        }

        // Проверяем конкретные панели по имени
        string[] panelNames = { "BuildingPanel", "ResourcePanel", "SelectionPanel", "ToggleButton" };
        foreach (string name in panelNames)
        {
            Transform panel = canvas.transform.FindDeepChild(name);
            if (panel != null)
            {
                Debug.Log($"Панель '{name}': найдена, активна={panel.gameObject.activeSelf}");
                DrawDebugInfo(panel as RectTransform);
            }
            else
            {
                Debug.LogWarning($"Панель '{name}' не найдена на Canvas!");
            }
        }
    }

    string GetUIElementStatus(RectTransform rt)
    {
        GameObject go = rt.gameObject;

        string status = $"{go.name}: ";
        status += $"Активен={go.activeSelf}, ";
        status += $"В иерархии={go.activeInHierarchy}, ";

        // Проверяем компоненты
        Image img = go.GetComponent<Image>();
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        Text legacyText = go.GetComponent<Text>();

        if (img != null)
        {
            status += $"Image(alpha={img.color.a}), ";
        }
        if (text != null)
        {
            status += $"TMP(alpha={text.alpha}), ";
        }
        if (legacyText != null)
        {
            status += $"Text(alpha={legacyText.color.a}), ";
        }

        // Позиция и размер
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        status += $"Позиция={rt.position}, Размер={rt.sizeDelta}, Мир: {corners[0]} -> {corners[2]}";

        return status;
    }

    bool IsRectTransformOffScreen(RectTransform rt)
    {
        // Преобразуем углы RectTransform в мировые координаты
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        // Проверяем, все ли углы за пределами экрана
        int offScreenCount = 0;
        foreach (Vector3 corner in corners)
        {
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(corner);
            if (screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1)
            {
                offScreenCount++;
            }
        }

        return offScreenCount == 4; // Все 4 угла за экраном
    }

    void DrawDebugInfo(RectTransform rt)
    {
        if (rt == null) return;

        // Получаем мировые координаты углов
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        Debug.Log($"{rt.name}:");
        Debug.Log($"  Anchors: Min={rt.anchorMin}, Max={rt.anchorMax}");
        Debug.Log($"  Pivot: {rt.pivot}");
        Debug.Log($"  Pos: {rt.anchoredPosition}");
        Debug.Log($"  Size: {rt.sizeDelta}");
        Debug.Log($"  Scale: {rt.localScale}");

        // Проверяем цвет/альфу
        Graphic graphic = rt.GetComponent<Graphic>();
        if (graphic != null)
        {
            Debug.Log($"  Color: {graphic.color}, Alpha={graphic.color.a}");
            if (graphic.color.a < 0.01f)
            {
                Debug.LogError($"  ❌ ПРОБЛЕМА: Альфа={graphic.color.a} (почти прозрачно)!");
            }
        }

        Debug.Log($"  Мировые координаты углов:");
        for (int i = 0; i < 4; i++)
        {
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(corners[i]);
            Debug.Log($"    Угол {i}: мир={corners[i]}, экран={screenPoint}");
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || canvas == null) return;

        // Рисуем рамки вокруг всех UI элементов
        RectTransform[] allUI = canvas.GetComponentsInChildren<RectTransform>();
        foreach (RectTransform rt in allUI)
        {
            if (rt == canvas.transform) continue;

            Gizmos.color = gizmoColor;
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            // Рисуем прямоугольник
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }

            // Точка в центре
            Gizmos.DrawSphere(rt.position, 2f);
        }
    }
}

// Расширение для поиска дочерних объектов по имени (включая вложенные)
public static class TransformExtensions
{
    public static Transform FindDeepChild(this Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result = child.FindDeepChild(name);
            if (result != null)
                return result;
        }
        return null;
    }
}